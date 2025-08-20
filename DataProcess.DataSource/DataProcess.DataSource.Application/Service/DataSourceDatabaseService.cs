using Furion.DynamicApiController;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;

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