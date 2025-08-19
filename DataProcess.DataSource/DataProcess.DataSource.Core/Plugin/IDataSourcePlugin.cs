namespace DataProcess.DataSource.Core.Plugin;

/// <summary>
/// ����Դ����������ӿ�
/// </summary>
public interface IDataSourcePlugin
{
    bool TestConnection(string configJson);
    object GetSchema(string configJson);
    object Query(string configJson, string dsl);
    int Write(string configJson, string dsl);
}