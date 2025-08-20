using Furion.DynamicApiController;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Adapter;
using DataProcess.DataSource.Core.Models;
using DataProcess.DataSource.Core.Plugin;
using SqlSugar;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// 数据库操作服务
/// </summary>
[ApiDescriptionSettings(Order = 120, Name = "数据库操作")]
public class DataSourceDatabaseService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly PluginManager _pluginManager;

    public DataSourceDatabaseService(ISqlSugarClient db, PluginManager pluginManager)
    {
        _db = db;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// 获取数据库列表
    /// </summary>
    [HttpGet]
    public async Task<List<string>> GetDatabaseList(long instanceId)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.GetDatabaseListAsync(instance.ConfigJson);
    }

    /// <summary>
    /// 创建数据库
    /// </summary>
    [HttpPost]
    public async Task<bool> CreateDatabase(long instanceId, string databaseName)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.CreateDatabaseAsync(instance.ConfigJson, databaseName);
    }

    /// <summary>
    /// 删除数据库
    /// </summary>
    [HttpPost]
    public async Task<bool> DropDatabase(long instanceId, string databaseName)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.DropDatabaseAsync(instance.ConfigJson, databaseName);
    }

    /// <summary>
    /// 获取表列表
    /// </summary>
    [HttpGet]
    public async Task<List<DataSourceTable>> GetTableList(long instanceId)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.GetTableListAsync(instance.ConfigJson);
    }

    /// <summary>
    /// 创建表
    /// </summary>
    [HttpPost]
    public async Task<bool> CreateTable(long instanceId, DataSourceTableSchema tableSchema)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.CreateTableAsync(instance.ConfigJson, tableSchema);
    }

    /// <summary>
    /// 删除表
    /// </summary>
    [HttpPost]
    public async Task<bool> DropTable(long instanceId, string tableName)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.DropTableAsync(instance.ConfigJson, tableName);
    }

    /// <summary>
    /// 获取表结构
    /// </summary>
    [HttpGet]
    public async Task<DataSourceTableSchema> GetTableSchema(long instanceId, string tableName)
    {
        var adapter = await GetAdapterAsync(instanceId);
        var instance = await GetInstanceAsync(instanceId);
        return await adapter.GetTableSchemaAsync(instance.ConfigJson, tableName);
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
            var adapter = _pluginManager.GetAdapter(instance.Type.AssemblyName, instance.Type.AdapterClassName);
            if (adapter == null)
                throw Oops.Oh("无法创建数据源适配器");
            return adapter;
        }
    }

    // 合并父→子配置，并回填到 ConfigJson
    private async Task<DataSourceInstance> GetInstanceAsync(long instanceId)
    {
        var instance = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.Id == instanceId)
            .FirstAsync();

        if (instance == null)
            throw Oops.Oh("数据源实例不存在");

        // 收集父链
        var chain = new List<DataSourceInstance>();
        var cursor = instance;
        while (cursor.ParentId.HasValue)
        {
            var parent = await _db.Queryable<DataSourceInstance>()
                .Where(i => i.Id == cursor.ParentId.Value)
                .FirstAsync();
            if (parent == null) break;
            chain.Add(parent);
            cursor = parent;
        }
        chain.Reverse(); // 父→子

        // 依次合并：父.Parameters/ConfigJson -> ... -> 子.Parameters/OverrideJson/ConfigJson
        var jsons = new List<string?>();
        foreach (var p in chain)
        {
            jsons.Add(string.IsNullOrWhiteSpace(p.ConfigJson) ? p.Parameters : p.ConfigJson);
        }
        jsons.Add(string.IsNullOrWhiteSpace(instance.ConfigJson) ? instance.Parameters : instance.ConfigJson);
        if (!string.IsNullOrWhiteSpace(instance.OverrideJson))
            jsons.Add(instance.OverrideJson);

        instance.ConfigJson = MergeConfigJson(jsons);
        return instance;
    }

    private static string MergeConfigJson(IEnumerable<string?> jsons)
    {
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var j in jsons)
        {
            if (string.IsNullOrWhiteSpace(j)) continue;
            var dict = JSON.Deserialize<Dictionary<string, object?>>(j) ?? new Dictionary<string, object?>();
            foreach (var kv in dict)
            {
                merged[kv.Key] = kv.Value;
            }
        }
        return JSON.Serialize(merged);
    }
}