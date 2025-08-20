using DataProcess.DataSource.Core.Models;


namespace DataProcess.DataSource.Core.Plugin;

/// <summary>
/// ����Դ�������ӿ�
/// </summary>
public interface IDataSourceAdapter
{
    /// <summary>
    /// ��������
    /// </summary>
    Task<bool> TestConnectionAsync(string configJson);

    /// <summary>
    /// ��ȡ���ݿ�Schema
    /// </summary>
    Task<DataSourceSchema> GetSchemaAsync(string configJson);

    /// <summary>
    /// ִ��DSL��ѯ
    /// </summary>
    Task<DataSourceResult> QueryAsync(string configJson, DataSourceQuery query);

    /// <summary>
    /// ִ��DSLд��
    /// </summary>
    Task<int> WriteAsync(string configJson, DataSourceWrite write);

    /// <summary>
    /// �������ݿ�
    /// </summary>
    Task<bool> CreateDatabaseAsync(string configJson, string databaseName);

    /// <summary>
    /// ɾ�����ݿ�
    /// </summary>
    Task<bool> DropDatabaseAsync(string configJson, string databaseName);

    /// <summary>
    /// ��ȡ���ݿ��б�
    /// </summary>
    Task<List<string>> GetDatabaseListAsync(string configJson);

    /// <summary>
    /// ��ȡ���б�
    /// </summary>
    Task<List<DataSourceTable>> GetTableListAsync(string configJson);

    /// <summary>
    /// ������
    /// </summary>
    Task<bool> CreateTableAsync(string configJson, DataSourceTableSchema tableSchema);

    /// <summary>
    /// ɾ����
    /// </summary>
    Task<bool> DropTableAsync(string configJson, string tableName);

    /// <summary>
    /// ��ȡ��ṹ
    /// </summary>
    Task<DataSourceTableSchema> GetTableSchemaAsync(string configJson, string tableName);
}