namespace DataProcess.DataSource.Core.Models;

/// <summary>
/// 数据源Schema
/// </summary>
public class DataSourceSchema
{
    public List<DataSourceDatabase> Databases { get; set; } = new();
    public List<DataSourceTable> Tables { get; set; } = new();
}

/// <summary>
/// 数据库信息
/// </summary>
public class DataSourceDatabase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DataSourceTable> Tables { get; set; } = new();
}

/// <summary>
/// 数据表信息
/// </summary>
public class DataSourceTable
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DataSourceColumn> Columns { get; set; } = new();
}

/// <summary>
/// 数据列信息
/// </summary>
public class DataSourceColumn
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public string Description { get; set; } = string.Empty;
    public object? DefaultValue { get; set; }
}

/// <summary>
/// 表结构定义
/// </summary>
public class DataSourceTableSchema
{
    public string TableName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DataSourceColumn> Columns { get; set; } = new();
    public List<DataSourceIndex> Indexes { get; set; } = new();
}

/// <summary>
/// 索引信息
/// </summary>
public class DataSourceIndex
{
    public string Name { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; }
}

/// <summary>
/// DSL查询对象
/// </summary>
public class DataSourceQuery
{
    public string Table { get; set; } = string.Empty;
    public List<string> Select { get; set; } = new();
    public DataSourceWhere? Where { get; set; }
    public List<DataSourceJoin> Joins { get; set; } = new();
    public List<DataSourceOrderBy> OrderBy { get; set; } = new();
    public DataSourceGroupBy? GroupBy { get; set; }
    public DataSourceHaving? Having { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

/// <summary>
/// 条件对象
/// </summary>
public class DataSourceWhere
{
    public string Logic { get; set; } = "and"; // and, or
    public List<DataSourceCondition> Conditions { get; set; } = new();
    public List<DataSourceWhere> Groups { get; set; } = new();
}

/// <summary>
/// 单个条件
/// </summary>
public class DataSourceCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty; // eq, ne, gt, gte, lt, lte, like, in, between, is_null, is_not_null
    public object? Value { get; set; }
    public object? Value2 { get; set; } // for between
}

/// <summary>
/// 连接查询
/// </summary>
public class DataSourceJoin
{
    public string Type { get; set; } = string.Empty; // inner, left, right, full
    public string Table { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string On { get; set; } = string.Empty;
}

/// <summary>
/// 排序
/// </summary>
public class DataSourceOrderBy
{
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = "asc"; // asc, desc
}

/// <summary>
/// 分组
/// </summary>
public class DataSourceGroupBy
{
    public List<string> Fields { get; set; } = new();
}

/// <summary>
/// Having条件
/// </summary>
public class DataSourceHaving : DataSourceWhere
{
}

/// <summary>
/// 写入数据对象
/// </summary>
public class DataSourceWrite
{
    public string Table { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty; // insert, update, delete, upsert
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public DataSourceWhere? Where { get; set; } // for update/delete
    public List<string> KeyFields { get; set; } = new(); // for upsert
}

/// <summary>
/// 查询结果
/// </summary>
public class DataSourceResult
{
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public string[] Columns { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Summary { get; set; } = new();
}

/// <summary>
/// SqlSugar连接配置
/// </summary>
public class SqlSugarConnectionConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public SqlSugar.DbType DbType { get; set; }
}