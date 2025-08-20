using DataProcess.DataSource.Core.Models;


namespace DataProcess.DataSource.Core.Plugin;

/// <summary>
/// 数据源适配器接口
/// </summary>
public interface IDataSourceAdapter
{
    /// <summary>
    /// 测试连接
    /// </summary>
    Task<bool> TestConnectionAsync(string configJson);

    /// <summary>
    /// 获取数据库Schema
    /// </summary>
    Task<DataSourceSchema> GetSchemaAsync(string configJson);

    /// <summary>
    /// 执行DSL查询
    /// </summary>
    Task<DataSourceResult> QueryAsync(string configJson, DataSourceQuery query);

    /// <summary>
    /// 执行DSL写入
    /// </summary>
    Task<int> WriteAsync(string configJson, DataSourceWrite write);

    /// <summary>
    /// 创建数据库
    /// </summary>
    Task<bool> CreateDatabaseAsync(string configJson, string databaseName);

    /// <summary>
    /// 删除数据库
    /// </summary>
    Task<bool> DropDatabaseAsync(string configJson, string databaseName);

    /// <summary>
    /// 获取数据库列表
    /// </summary>
    Task<List<string>> GetDatabaseListAsync(string configJson);

    /// <summary>
    /// 获取表列表
    /// </summary>
    Task<List<DataSourceTable>> GetTableListAsync(string configJson);

    /// <summary>
    /// 创建表
    /// </summary>
    Task<bool> CreateTableAsync(string configJson, DataSourceTableSchema tableSchema);

    /// <summary>
    /// 删除表
    /// </summary>
    Task<bool> DropTableAsync(string configJson, string tableName);

    /// <summary>
    /// 获取表结构
    /// </summary>
    Task<DataSourceTableSchema> GetTableSchemaAsync(string configJson, string tableName);
}