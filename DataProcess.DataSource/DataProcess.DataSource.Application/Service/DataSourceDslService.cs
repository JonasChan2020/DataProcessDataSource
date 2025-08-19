using Furion.DynamicApiController;
using SqlSugar;
using DataProcess.DataSource.Application.Dto;
using DataProcess.DataSource.Application.Utils;
using DataProcess.DataSource.Application.Service.Plugin;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// DSL 查询服务
/// </summary>
[ApiDescriptionSettings(Order = 210, Name = "DSL查询")]
public class DataSourceDslService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly PluginSandboxManager _pluginManager;

    public DataSourceDslService(ISqlSugarClient db, PluginSandboxManager pluginManager)
    {
        _db = db;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// 统一DSL查询（带结构校验和插件沙箱）
    /// </summary>
    public async Task<ApiResponse<object>> Query([FromBody] DslQueryDto input)
    {
        DslSchemaValidator.Validate(input.Dsl);

        var instance = await _db.Queryable<DataSourceInstance>().InSingleAsync(input.InstanceId);
        if (instance == null) throw Oops.Oh("数据源实例不存在");

        var type = await _db.Queryable<DataSourceType>().InSingleAsync(instance.TypeId);
        if (type == null) throw Oops.Oh("数据源类型不存在");

        var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "datasource", type.PluginAssembly, $"{type.PluginAssembly}.dll");
        var plugin = _pluginManager.LoadPlugin(pluginPath, type.PluginAssembly);
        if (plugin == null) throw Oops.Oh("插件未加载");

        try
        {
            var result = plugin.Query(instance.ConfigJson, input.Dsl);
            return ApiResponse<object>.Success(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("插件查询异常: " + ex.Message);
        }
    }
}