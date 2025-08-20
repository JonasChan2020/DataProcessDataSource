using Furion.DynamicApiController;
using SqlSugar;
using DataProcess.DataSource.Application.Dto;
using DataProcess.DataSource.Application.Utils;
using DataProcess.DataSource.Application.Service.Plugin;
using DataProcess.DataSource.Application.Entity;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// DSL ��ѯ����
/// </summary>
[ApiDescriptionSettings(Order = 210, Name = "DSL��ѯ")]
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
    /// ͳһDSL��ѯ�����ṹУ��Ͳ��ɳ�䣩
    /// </summary>
    public async Task<ApiResponse<object>> Query([FromBody] DslQueryDto input)
    {
        DslSchemaValidator.Validate(input.Dsl);

        var instance = await _db.Queryable<DataSourceInstance>().InSingleAsync(input.InstanceId);
        if (instance == null) throw Oops.Oh("����Դʵ��������");

        var type = await _db.Queryable<DataSourceType>().InSingleAsync(instance.TypeId);
        if (type == null) throw Oops.Oh("����Դ���Ͳ�����");

        var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "datasource", type.PluginAssembly, $"{type.PluginAssembly}.dll");
        var plugin = _pluginManager.LoadPlugin(pluginPath, type.PluginAssembly);
        if (plugin == null) throw Oops.Oh("���δ����");

        try
        {
            var result = plugin.Query(instance.ConfigJson, input.Dsl);
            return ApiResponse<object>.Success(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("�����ѯ�쳣: " + ex.Message);
        }
    }
}