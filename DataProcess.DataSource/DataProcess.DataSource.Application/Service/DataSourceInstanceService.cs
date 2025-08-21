using Furion.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Mapster;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;
using DataProcess.DataSource.Application.Service.Adapter;
using DataProcess.DataSource.Core.Plugin;
using DataProcess.DataSource.Core.Paging;
using DataProcess.DataSource.Application.Utils;

namespace DataProcess.DataSource.Application.Service;

[ApiDescriptionSettings("数据源", Order = 2, Name = "数据源实例")]
public class DataSourceInstanceService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly PluginManager _plugin;

    public DataSourceInstanceService(ISqlSugarClient db, PluginManager plugin)
    {
        _db = db;
        _plugin = plugin;
    }

    [HttpPost]
    public async Task<SqlSugarPagedList<DataSourceInstanceDto>> Page(DataSourceInstancePageInput input)
    {
        var q = _db.Queryable<DataSourceInstance>()
            .LeftJoin<DataSourceType>((i, t) => i.TypeId == t.Id)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), (i, t) => i.Code.Contains(input.Code!))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), (i, t) => i.Name.Contains(input.Name!))
            .WhereIF(!string.IsNullOrWhiteSpace(input.TypeCode), (i, t) => t.Code == input.TypeCode)
            .WhereIF(input.Enabled.HasValue, (i, t) => i.Enabled == input.Enabled)
            .OrderBy("i.CreateTime desc");

        var paged = await q.Select((i, t) => new DataSourceInstanceDto
        {
            Id = i.Id, Code = i.Code, Name = i.Name,
            TypeId = i.TypeId, TypeCode = i.TypeCode, TypeName = t.Name,
            ParentId = i.ParentId, OverrideJson = i.OverrideJson,
            Parameters = i.Parameters, ConfigJson = i.ConfigJson,
            Enabled = i.Enabled, Remark = i.Remark,
            CreateTime = i.CreateTime, UpdateTime = i.UpdateTime
        }).ToPagedListAsync(input.Page, input.PageSize);

        return paged;
    }

    [HttpPost]
    public async Task<long> Add(DataSourceInstanceInput input)
    {
        if (await _db.Queryable<DataSourceInstance>().AnyAsync(x => x.Code == input.Code))
            throw Oops.Oh("实例编码已存在");

        var entity = input.Adapt<DataSourceInstance>();
        if (entity.TypeId == null && !string.IsNullOrWhiteSpace(entity.TypeCode))
        {
            var t = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Code == entity.TypeCode);
            entity.TypeId = t?.Id;
        }
        entity.ConfigJson = SensitiveConfigProtector.EncryptSensitiveFields(entity.ConfigJson ?? entity.Parameters);
        entity.CreateTime = DateTime.Now;

        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    [HttpPost]
    public async Task Update(DataSourceInstanceUpdateInput input)
    {
        var entity = await _db.Queryable<DataSourceInstance>().FirstAsync(x => x.Id == input.Id)
                     ?? throw Oops.Oh("实例不存在");

        if (!string.Equals(entity.Code, input.Code, StringComparison.OrdinalIgnoreCase) &&
            await _db.Queryable<DataSourceInstance>().AnyAsync(x => x.Code == input.Code))
            throw Oops.Oh("实例编码已存在");

        input.Adapt(entity);
        if (entity.TypeId == null && !string.IsNullOrWhiteSpace(entity.TypeCode))
        {
            var t = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Code == entity.TypeCode);
            entity.TypeId = t?.Id;
        }
        entity.ConfigJson = SensitiveConfigProtector.EncryptSensitiveFields(entity.ConfigJson ?? entity.Parameters);
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    [HttpPost]
    public async Task Delete(BaseIdInput input)
    {
        await _db.Deleteable<DataSourceInstance>().In(input.Id).ExecuteCommandAsync();
    }

    [HttpGet]
    public async Task<DataSourceInstanceDto> Detail(long id)
    {
        var data = await _db.Queryable<DataSourceInstance>()
            .LeftJoin<DataSourceType>((i, t) => i.TypeId == t.Id)
            .Where((i, _) => i.Id == id)
            .Select((i, t) => new DataSourceInstanceDto
            {
                Id = i.Id, Code = i.Code, Name = i.Name,
                TypeId = i.TypeId, TypeCode = i.TypeCode, TypeName = t.Name,
                ParentId = i.ParentId, OverrideJson = i.OverrideJson,
                Parameters = i.Parameters, ConfigJson = i.ConfigJson,
                Enabled = i.Enabled, Remark = i.Remark,
                CreateTime = i.CreateTime, UpdateTime = i.UpdateTime
            }).FirstAsync();

        return data ?? throw Oops.Oh("实例不存在");
    }

    [HttpPost]
    public async Task<bool> TestConnection(BaseIdInput input)
    {
        var instance = await GetInstanceAsync(input.Id);
        var adapter = await GetAdapterAsync(instance.TypeId!.Value);
        return await adapter.TestConnectionAsync(instance.ConfigJson!);
    }

    private async Task<IDataSourceAdapter> GetAdapterAsync(long typeId)
    {
        var type = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Id == typeId)
                   ?? throw Oops.Oh("类型不存在");

        if (type.IsBuiltIn) return new SqlSugarDataSourceAdapter();

        var adapter = _plugin.GetAdapter(type.AssemblyName, type.AdapterClassName);
        return adapter ?? throw Oops.Oh("无法创建数据源适配器");
    }

    private async Task<DataSourceInstance> GetInstanceAsync(long id)
    {
        var ins = await _db.Queryable<DataSourceInstance>().FirstAsync(x => x.Id == id)
                  ?? throw Oops.Oh("实例不存在");

        var chain = new List<DataSourceInstance>();
        var cursor = ins;
        while (cursor.ParentId.HasValue)
        {
            var parent = await _db.Queryable<DataSourceInstance>().FirstAsync(x => x.Id == cursor.ParentId.Value);
            if (parent == null) break;
            chain.Add(parent);
            cursor = parent;
        }
        chain.Reverse();

        var jsons = new List<string?>();
        foreach (var p in chain) jsons.Add(string.IsNullOrWhiteSpace(p.ConfigJson) ? p.Parameters : p.ConfigJson);
        jsons.Add(string.IsNullOrWhiteSpace(ins.ConfigJson) ? ins.Parameters : ins.ConfigJson);
        if (!string.IsNullOrWhiteSpace(ins.OverrideJson)) jsons.Add(ins.OverrideJson);

        var merged = Merge(jsons);
        ins.ConfigJson = SensitiveConfigProtector.DecryptSensitiveFields(merged);
        return ins;

        static string Merge(IEnumerable<string?> xs)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var j in xs)
            {
                if (string.IsNullOrWhiteSpace(j)) continue;
                var d = JSON.Deserialize<Dictionary<string, object?>>(j!) ?? new();
                foreach (var kv in d) dict[kv.Key] = kv.Value;
            }
            return JSON.Serialize(dict);
        }
    }
}