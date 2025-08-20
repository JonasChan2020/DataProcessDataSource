using SqlSugar;
using DataProcess.DataSource.Core.Plugin;
using DataProcess.DataSource.Core.Models;
using System.Data;
using System.Text;

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

            var db = new SqlSugarClient(new ConnectionConfig
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
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
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
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        var sql = BuildSqlFromQuery(query);
        var dataTable = await db.Ado.GetDataTableAsync(sql);
        
        var result = new DataSourceResult
        {
            TotalCount = dataTable.Rows.Count,
            Columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray()
        };
        
        foreach (DataRow row in dataTable.Rows)
        {
            var dict = new Dictionary<string, object>();
            foreach (var column in result.Columns)
            {
                dict[column] = row[column] == DBNull.Value ? null! : row[column];
            }
            result.Data.Add(dict);
        }

        return result;
    }

    public async Task<int> WriteAsync(string configJson, DataSourceWrite write)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
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
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        return await db.DbMaintenance.CreateDatabaseAsync(databaseName);
    }

    public async Task<bool> DropDatabaseAsync(string configJson, string databaseName)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        await db.Ado.ExecuteCommandAsync($"DROP DATABASE IF EXISTS {databaseName}");
        return true;
    }

    public async Task<List<string>> GetDatabaseListAsync(string configJson)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        var dataTable = await db.Ado.GetDataTableAsync("SHOW DATABASES");
        return dataTable.Rows.Cast<DataRow>().Select(row => row[0]?.ToString() ?? "").ToList();
    }

    public async Task<List<DataSourceTable>> GetTableListAsync(string configJson)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
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
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
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
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = config.ConnectionString,
            DbType = config.DbType,
            IsAutoCloseConnection = true
        });

        return await db.DbMaintenance.DropTableAsync(tableName);
    }

    public async Task<DataSourceTableSchema> GetTableSchemaAsync(string configJson, string tableName)
    {
        var config = JSON.Deserialize<SqlSugarConnectionConfig>(configJson);
        if (config == null) throw new Exception("配置格式错误");

        var db = new SqlSugarClient(new ConnectionConfig
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

    private string BuildSqlFromQuery(DataSourceQuery query)
    {
        var sql = new StringBuilder();
        
        // SELECT
        if (query.Select.Any())
        {
            sql.Append($"SELECT {string.Join(", ", query.Select)} ");
        }
        else
        {
            sql.Append("SELECT * ");
        }
        
        // FROM
        sql.Append($"FROM {query.Table} ");
        
        // WHERE
        if (query.Where != null)
        {
            var whereClause = BuildWhereClause(query.Where);
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql.Append($"WHERE {whereClause} ");
            }
        }
        
        // ORDER BY
        if (query.OrderBy.Any())
        {
            sql.Append("ORDER BY ");
            sql.Append(string.Join(", ", query.OrderBy.Select(o => $"{o.Field} {o.Direction}")));
            sql.Append(" ");
        }
        
        // LIMIT
        if (query.Limit.HasValue)
        {
            sql.Append($"LIMIT {query.Limit.Value} ");
            if (query.Offset.HasValue)
            {
                sql.Append($"OFFSET {query.Offset.Value} ");
            }
        }
        
        return sql.ToString();
    }

    private string BuildWhereClause(DataSourceWhere where)
    {
        var conditions = new List<string>();

        foreach (var condition in where.Conditions)
        {
            var clause = condition.Operator.ToLower() switch
            {
                "eq" => $"{condition.Field} = '{condition.Value}'",
                "ne" => $"{condition.Field} != '{condition.Value}'",
                "gt" => $"{condition.Field} > '{condition.Value}'",
                "gte" => $"{condition.Field} >= '{condition.Value}'",
                "lt" => $"{condition.Field} < '{condition.Value}'",
                "lte" => $"{condition.Field} <= '{condition.Value}'",
                "like" => $"{condition.Field} LIKE '%{condition.Value}%'",
                "is_null" => $"{condition.Field} IS NULL",
                "is_not_null" => $"{condition.Field} IS NOT NULL",
                _ => $"{condition.Field} = '{condition.Value}'"
            };
            conditions.Add(clause);
        }

        foreach (var group in where.Groups)
        {
            var groupClause = BuildWhereClause(group);
            if (!string.IsNullOrEmpty(groupClause))
                conditions.Add($"({groupClause})");
        }

        return string.Join($" {where.Logic.ToUpper()} ", conditions);
    }

    private async Task<int> InsertData(ISqlSugarClient db, DataSourceWrite write)
    {
        if (!write.Data.Any()) return 0;

        var insertCount = 0;
        foreach (var item in write.Data)
        {
            insertCount += await db.Insertable(item).AS(write.Table).ExecuteCommandAsync();
        }
        return insertCount;
    }

    private async Task<int> UpdateData(ISqlSugarClient db, DataSourceWrite write)
    {
        if (!write.Data.Any()) return 0;

        var updateCount = 0;
        foreach (var item in write.Data)
        {
            var updateable = db.Updateable(item).AS(write.Table);
            if (write.Where != null)
            {
                var whereClause = BuildWhereClause(write.Where);
                if (!string.IsNullOrEmpty(whereClause))
                    updateable = updateable.Where(whereClause);
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
            var whereClause = BuildWhereClause(write.Where);
            if (!string.IsNullOrEmpty(whereClause))
                deleteable = deleteable.Where(whereClause);
        }
        return await deleteable.ExecuteCommandAsync();
    }
}