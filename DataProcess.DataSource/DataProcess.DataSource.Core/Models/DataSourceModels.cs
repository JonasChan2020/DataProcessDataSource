using SqlSugar;
using System;
using System.Collections.Generic;
using DbType = SqlSugar.DbType;

namespace DataProcess.DataSource.Core.Models;

/// <summary>
/// ����ԴSchema
/// </summary>
public class DataSourceSchema
{
    public List<DataSourceDatabase> Databases { get; set; } = new();
    public List<DataSourceTable> Tables { get; set; } = new();
}

/// <summary>
/// ���ݿ���Ϣ
/// </summary>
public class DataSourceDatabase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DataSourceTable> Tables { get; set; } = new();
}

/// <summary>
/// ���ݱ���Ϣ
/// </summary>
public class DataSourceTable
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DataSourceColumn> Columns { get; set; } = new();
}

/// <summary>
/// ��������Ϣ
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
/// ���ṹ����
/// </summary>
public class DataSourceTableSchema
{
    public string TableName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DataSourceColumn> Columns { get; set; } = new();
    public List<DataSourceIndex> Indexes { get; set; } = new();
}

/// <summary>
/// ������Ϣ
/// </summary>
public class DataSourceIndex
{
    public string Name { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; }
}

/// <summary>
/// DSL��ѯ����
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
/// ��������
/// </summary>
public class DataSourceWhere
{
    public string Logic { get; set; } = "and"; // and, or
    public List<DataSourceCondition> Conditions { get; set; } = new();
    public List<DataSourceWhere> Groups { get; set; } = new();
}

/// <summary>
/// ��������
/// </summary>
public class DataSourceCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty; // eq, ne, gt, gte, lt, lte, like, in, between, is_null, is_not_null
    public object? Value { get; set; }
    public object? Value2 { get; set; } // for between
}

/// <summary>
/// ���Ӳ�ѯ
/// </summary>
public class DataSourceJoin
{
    public string Type { get; set; } = string.Empty; // inner, left, right, full
    public string Table { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string On { get; set; } = string.Empty;
}

/// <summary>
/// ����
/// </summary>
public class DataSourceOrderBy
{
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = "asc"; // asc, desc
}

/// <summary>
/// ����
/// </summary>
public class DataSourceGroupBy
{
    public List<string> Fields { get; set; } = new();
}

/// <summary>
/// Having����
/// </summary>
public class DataSourceHaving : DataSourceWhere
{
}

/// <summary>
/// д�����ݶ���
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
/// ��ѯ���
/// </summary>
public class DataSourceResult
{
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public string[] Columns { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Summary { get; set; } = new();
}

/// <summary>
/// SqlSugar��������
/// </summary>
public class SqlSugarConnectionConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public DbType DbType { get; set; }
}