using Furion.DynamicApiController;
using DataProcess.DataSource.Core.Models;
using DataProcess.DataSource.Application.Service.Plugin;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Adapter;
using SqlSugar;
using DataProcess.DataSource.Core.Plugin;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// 数据查询服务
/// </summary>
[ApiDescriptionSettings(Order = 130, Name = "数据查询")]
public class DataSourceQueryService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly PluginManager _pluginManager;

    public DataSourceQueryService(ISqlSugarClient db, PluginManager pluginManager)
    {
        _db = db;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// 统一DSL查询
    /// </summary>
    [HttpPost]
    public async Task<DataSourceResult> Query(long instanceId, DataSourceQuery query)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.QueryAsync(instance.ConfigJson, query);
    }

    /// <summary>
    /// 统一DSL写入
    /// </summary>
    [HttpPost]
    public async Task<int> Write(long instanceId, DataSourceWrite write)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.WriteAsync(instance.ConfigJson, write);
    }

    /// <summary>
    /// 获取Schema
    /// </summary>
    [HttpGet]
    public async Task<DataSourceSchema> GetSchema(long instanceId)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.GetSchemaAsync(instance.ConfigJson);
    }

    /// <summary>
    /// 批量查询（多数据源并发）
    /// </summary>
    [HttpPost]
    public async Task<List<DataSourceResult>> BatchQuery(List<BatchQueryInput> inputs)
    {
        var tasks = inputs.Select(async input =>
        {
            var adapter = await GetAdapterAsync(input.InstanceId);
            var instance = await GetInstanceAsync(input.InstanceId);
            return await adapter.QueryAsync(instance.ConfigJson, input.Query);
        });
        return (await Task.WhenAll(tasks)).ToList();
    }

    private async Task<IDataSourceAdapter> GetAdapterAsync(long instanceId)
    {
        var instance = await _db.Queryable<DataSourceInstance>()
            .LeftJoin<DataSourceType>((i, t) => i.TypeId == t.Id)
            .Where((i, t) => i.Id == instanceId)
            .Select((i, t) => new { Instance = i, Type = t })
            .FirstAsync();

        if (instance == null)
            throw Oops.Oh("数据源实例不存在");

        if (instance.Type.IsBuiltIn)
        {
            return new SqlSugarDataSourceAdapter();
        }
        else
        {
            var adapter = _pluginManager.GetAdapter(instance.Type.PluginAssembly, instance.Type.AdapterClassName);
            if (adapter == null)
                throw Oops.Oh("无法创建数据源适配器");
            return adapter;
        }
    }

    private async Task<DataSourceInstance> GetInstanceAsync(long instanceId)
    {
        var instance = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.Id == instanceId)
            .FirstAsync();

        if (instance == null)
            throw Oops.Oh("数据源实例不存在");

        return instance;
    }
}

/// <summary>
/// 批量查询输入
/// </summary>
public class BatchQueryInput
{
    public long InstanceId { get; set; }
    public DataSourceQuery Query { get; set; } = new();
}