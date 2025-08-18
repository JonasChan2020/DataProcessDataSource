namespace Common.Interfaces;

using Common.Models;

public interface IDataSourceAdapter
{
    Task<bool> TestConnectionAsync(DataSourceConfig config);
    Task<Schema?> GetSchemaAsync(DataSourceConfig config, string objectName);
    Task<List<Dictionary<string, object>>> ReadAsync(string query, DataSourceConfig config);
    Task<int> WriteAsync(string table, IEnumerable<Dictionary<string, object>> data, DataSourceConfig config);
}
