namespace DataProcess.DataSource.Core.Plugin;

/// <summary>
/// 数据源插件适配器接口
/// </summary>
public interface IDataSourcePlugin
{
    bool TestConnection(string configJson);
    object GetSchema(string configJson);
    object Query(string configJson, string dsl);
    int Write(string configJson, string dsl);
}