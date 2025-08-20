using Furion;
using SqlSugar;
using DataProcess.DataSource.Core.Plugin;
using DataProcess.DataSource.Core.Models;
using System.Data;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using DbType = SqlSugar.DbType;

namespace DataProcess.DataSource.Application.Service.Adapter;

/// <summary>
/// SqlSugar数据源适配器（用于内置数据库类型）
/// </summary>
public class SqlSugarDataSourceAdapter : IDataSourceAdapter
{
    public async Task<bool> TestConnectionAsync(string configJson)
    {
        try
        {
            var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
            if (config == null) return false;

            using var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = config.ConnectionString,
                DbType = config.DbType,
                IsAutoCloseConnection = true
            });

            await db.Ado.GetDataTableAsync("SELECT 1");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DataSourceSchema> GetSchemaAsync(string configJson)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        var schema = new DataSourceSchema();

        var database = new DataSourceDatabase
        {
            Name = db.Ado.Connection.Database ?? "Unknown",
            Description = "当前数据库"
        };

        var tables = db.DbMaintenance.GetTableInfoList(false);
        foreach (var table in tables)
        {
            var dataTable = new DataSourceTable
            {
                Name = table.Name,
                Description = table.Description ?? ""
            };

            var columns = db.DbMaintenance.GetColumnInfosByTableName(table.Name, false);
            foreach (var column in columns)
            {
                dataTable.Columns.Add(new DataSourceColumn
                {
                    Name = column.DbColumnName,
                    DataType = column.DataType,
                    Length = column.Length,
                    IsNullable = column.IsNullable,
                    IsPrimaryKey = column.IsPrimarykey,
                    IsIdentity = column.IsIdentity,
                    Description = column.ColumnDescription ?? "",
                    DefaultValue = column.DefaultValue
                });
            }

            database.Tables.Add(dataTable);
            schema.Tables.Add(dataTable);
        }

        schema.Databases.Add(database);
        return schema;
    }

    public async Task<DataSourceResult> QueryAsync(string configJson, DataSourceQuery query)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        var (sql, parameters) = BuildSqlFromQuery(query);
        var dataTable = await db.Ado.GetDataTableAsync(sql, parameters?.ToArray());

        var result = new DataSourceResult
        {
            TotalCount = dataTable.Rows.Count,
            Columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray()
        };

        foreach (DataRow row in dataTable.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var column in result.Columns)
            {
                dict[column] = row[column] == DBNull.Value ? null : row[column];
            }
            result.Data.Add(dict);
        }

        return result;
    }

    public async Task<int> WriteAsync(string configJson, DataSourceWrite write)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        return write.Operation.ToLower() switch
        {
            "insert" => await InsertData(db, write),
            "update" => await UpdateData(db, write),
            "delete" => await DeleteData(db, write),
            _ => throw new NotSupportedException($"不支持的操作类型: {write.Operation}")
        };
    }

    public async Task<bool> CreateDatabaseAsync(string configJson, string databaseName)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        string sql = config.DbType switch
        {
            DbType.SqlServer => $"IF DB_ID(N'{EscapeLiteral(databaseName)}') IS NULL CREATE DATABASE [{EscapeIdentifier(databaseName)}];",
            DbType.MySql or DbType.MySqlConnector => $"CREATE DATABASE IF NOT EXISTS `{EscapeIdentifier(databaseName)}`;",
            DbType.PostgreSQL => $"DO $$ BEGIN IF NOT EXISTS (SELECT FROM pg_database WHERE datname = '{EscapeLiteral(databaseName)}') THEN EXECUTE 'CREATE DATABASE \"{EscapeIdentifier(databaseName)}\"'; END IF; END $$;",
            DbType.Sqlite => throw new NotSupportedException("SQLite 无需单独建库，请直接使用数据库文件连接。"),
            DbType.Oracle => throw new NotSupportedException("Oracle 建库需 DBA 权限，请使用外部工具创建后再配置连接。"),
            _ => throw new NotSupportedException($"暂不支持 {config.DbType} 的自动建库。")
        };

        await db.Ado.ExecuteCommandAsync(sql);
        return true;
    }

    public async Task<bool> DropDatabaseAsync(string configJson, string databaseName)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        string sql = config.DbType switch
        {
            DbType.SqlServer => $"IF DB_ID(N'{EscapeLiteral(databaseName)}') IS NOT NULL DROP DATABASE [{EscapeIdentifier(databaseName)}];",
            DbType.MySql or DbType.MySqlConnector => $"DROP DATABASE IF EXISTS `{EscapeIdentifier(databaseName)}`;",
            DbType.PostgreSQL => $"DROP DATABASE IF EXISTS \"{EscapeIdentifier(databaseName)}\";",
            _ => throw new NotSupportedException($"暂不支持 {config.DbType} 的自动删库。")
        };

        await db.Ado.ExecuteCommandAsync(sql);
        return true;
    }

    public async Task<List<string>> GetDatabaseListAsync(string configJson)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        string sql = config.DbType switch
        {
            DbType.SqlServer => "SELECT name FROM sys.databases ORDER BY name",
            DbType.MySql or DbType.MySqlConnector => "SHOW DATABASES",
            DbType.PostgreSQL => "SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname",
            _ => throw new NotSupportedException($"暂不支持 {config.DbType} 的数据库列表查询")
        };

        var table = await db.Ado.GetDataTableAsync(sql);
        return table.Rows.Cast<DataRow>().Select(r => r[0]?.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    public async Task<List<DataSourceTable>> GetTableListAsync(string configJson)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        var tables = db.DbMaintenance.GetTableInfoList(false);
        return tables.Select(t => new DataSourceTable
        {
            Name = t.Name,
            Description = t.Description ?? ""
        }).ToList();
    }

    public async Task<bool> CreateTableAsync(string configJson, DataSourceTableSchema tableSchema)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        var columns = tableSchema.Columns.Select(c => new DbColumnInfo
        {
            DbColumnName = c.Name,
            DataType = c.DataType,
            Length = c.Length ?? 0,
            IsNullable = c.IsNullable,
            IsPrimarykey = c.IsPrimaryKey,
            IsIdentity = c.IsIdentity,
            ColumnDescription = c.Description
        }).ToList();

        db.DbMaintenance.CreateTable(tableSchema.TableName, columns);
        return await Task.FromResult(true);
    }

    public async Task<bool> DropTableAsync(string configJson, string tableName)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        try
        {
            var ok = db.DbMaintenance.DropTable(tableName);
            return await Task.FromResult(ok);
        }
        catch
        {
            var sql = config.DbType switch
            {
                DbType.SqlServer => $"IF OBJECT_ID(N'{EscapeLiteral(tableName)}', N'U') IS NOT NULL DROP TABLE [{EscapeIdentifier(tableName)}];",
                DbType.MySql or DbType.MySqlConnector => $"DROP TABLE IF EXISTS `{EscapeIdentifier(tableName)}`;",
                DbType.PostgreSQL => $"DROP TABLE IF EXISTS \"{EscapeIdentifier(tableName)}\";",
                _ => $"DROP TABLE {tableName}"
            };
            await db.Ado.ExecuteCommandAsync(sql);
            return true;
        }
    }

    public async Task<DataSourceTableSchema> GetTableSchemaAsync(string configJson, string tableName)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson) ?? throw new Exception("配置格式错误");

        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        var columns = db.DbMaintenance.GetColumnInfosByTableName(tableName, false);
        var schema = new DataSourceTableSchema
        {
            TableName = tableName,
            Description = db.DbMaintenance.GetTableInfoList(false).FirstOrDefault(t => t.Name == tableName)?.Description ?? ""
        };

        foreach (var column in columns)
        {
            schema.Columns.Add(new DataSourceColumn
            {
                Name = column.DbColumnName,
                DataType = column.DataType,
                Length = column.Length,
                IsNullable = column.IsNullable,
                IsPrimaryKey = column.IsPrimarykey,
                IsIdentity = column.IsIdentity,
                Description = column.ColumnDescription ?? "",
                DefaultValue = column.DefaultValue
            });
        }

        return schema;
    }

    private (string Sql, List<SugarParameter> Params) BuildSqlFromQuery(DataSourceQuery query)
    {
        var sql = new StringBuilder();
        var parameters = new List<SugarParameter>();
        var pIndex = 0;

        // SELECT
        if (query.Select != null && query.Select.Any())
            sql.Append($"SELECT {string.Join(", ", query.Select)} ");
        else
            sql.Append("SELECT * ");

        // FROM
        sql.Append($"FROM {query.Table} ");

        // WHERE
        if (query.Where != null)
        {
            var whereClause = BuildWhereClause(query.Where, parameters, ref pIndex);
            if (!string.IsNullOrWhiteSpace(whereClause))
                sql.Append("WHERE ").Append(whereClause).Append(' ');
        }

        // ORDER BY
        if (query.OrderBy != null && query.OrderBy.Any())
        {
            sql.Append("ORDER BY ");
            sql.Append(string.Join(", ", query.OrderBy.Select(o => $"{o.Field} {o.Direction}"))).Append(' ');
        }

        // LIMIT/OFFSET（MySQL/PG 语法；SQLServer 可通过 TOP/OFFSET FETCH，这里先保持通用）
        if (query.Limit.HasValue)
        {
            sql.Append($"LIMIT {query.Limit.Value} ");
            if (query.Offset.HasValue) sql.Append($"OFFSET {query.Offset.Value} ");
        }

        return (sql.ToString(), parameters);
    }

    private string BuildWhereClause(DataSourceWhere where, List<SugarParameter> parameters, ref int pIndex)
    {
        var parts = new List<string>();

        if (where.Conditions != null)
        {
            foreach (var c in where.Conditions)
            {
                var op = c.Operator?.ToLower() ?? "eq";
                if (op is "is_null" or "is_not_null")
                {
                    parts.Add($"{c.Field} {(op == "is_null" ? "IS NULL" : "IS NOT NULL")}");
                    continue;
                }

                var pName = $"@p{pIndex++}";
                var clause = op switch
                {
                    "eq" => $"{c.Field} = {pName}",
                    "ne" => $"{c.Field} <> {pName}",
                    "gt" => $"{c.Field} > {pName}",
                    "gte" => $"{c.Field} >= {pName}",
                    "lt" => $"{c.Field} < {pName}",
                    "lte" => $"{c.Field} <= {pName}",
                    "like" => $"{c.Field} LIKE {pName}",
                    _ => $"{c.Field} = {pName}"
                };

                var val = op == "like" ? $"%{c.Value}%" : c.Value;
                parameters.Add(new SugarParameter(pName, val));
                parts.Add(clause);
            }
        }

        if (where.Groups != null)
        {
            foreach (var g in where.Groups)
            {
                var inner = BuildWhereClause(g, parameters, ref pIndex);
                if (!string.IsNullOrWhiteSpace(inner))
                    parts.Add($"({inner})");
            }
        }

        var logic = string.IsNullOrWhiteSpace(where.Logic) ? "AND" : where.Logic.ToUpper();
        return string.Join($" {logic} ", parts);
    }

    private async Task<int> InsertData(ISqlSugarClient db, DataSourceWrite write)
    {
        if (write.Data == null || !write.Data.Any()) return 0;

        var insertCount = 0;
        foreach (var item in write.Data)
        {
            insertCount += await db.Insertable(item).AS(write.Table).ExecuteCommandAsync();
        }
        return insertCount;
    }

    private async Task<int> UpdateData(ISqlSugarClient db, DataSourceWrite write)
    {
        if (write.Data == null || !write.Data.Any()) return 0;

        var updateCount = 0;
        foreach (var item in write.Data)
        {
            var updateable = db.Updateable(item).AS(write.Table);
            if (write.Where != null)
            {
                var p = new List<SugarParameter>();
                var idx = 0;
                var whereClause = BuildWhereClause(write.Where, p, ref idx);
                if (!string.IsNullOrWhiteSpace(whereClause))
                    updateable = updateable.Where(whereClause, p);
            }
            updateCount += await updateable.ExecuteCommandAsync();
        }
        return updateCount;
    }

    private async Task<int> DeleteData(ISqlSugarClient db, DataSourceWrite write)
    {
        var deleteable = db.Deleteable<dynamic>().AS(write.Table);
        if (write.Where != null)
        {
            var p = new List<SugarParameter>();
            var idx = 0;
            var whereClause = BuildWhereClause(write.Where, p, ref idx);
            if (!string.IsNullOrWhiteSpace(whereClause))
                deleteable = deleteable.Where(whereClause, p);
        }
        return await deleteable.ExecuteCommandAsync();
    }

    private static string EscapeIdentifier(string name)
        => name.Replace("]", "]]").Replace("\"", "\"\"").Replace("`", "``");

    private static string EscapeLiteral(string value)
        => value.Replace("'", "''");
}