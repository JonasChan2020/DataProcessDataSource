using Furion.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Mapster;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Core.Paging;

namespace DataProcess.DataSource.Application.Service;

[ApiDescriptionSettings("数据源", Order = 1, Name = "数据源类型")]
public class DataSourceTypeService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;

    public DataSourceTypeService(ISqlSugarClient db) => _db = db;

    /// <summary>
    /// 分页查询数据源类型
    /// </summary>
    [HttpPost]
    public async Task<SqlSugarPagedList<DataSourceTypeDto>> Page(DataSourceTypePageInput input)
    {
        var q = _db.Queryable<DataSourceType>()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), x => x.Name.Contains(input.Name!))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), x => x.Code.Contains(input.Code!))
            .WhereIF(input.IsBuiltIn.HasValue, x => x.IsBuiltIn == input.IsBuiltIn)
            .WhereIF(input.Status.HasValue, x => x.Status == input.Status)
            .OrderBy(x => x.OrderNo)
            .OrderBy(x => x.Code);
        var page = await q.Select(x => new DataSourceTypeDto
        {
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Version = x.Version,
            AdapterClassName = x.AdapterClassName,
            AssemblyName = x.AssemblyName,
            ParamTemplate = x.ParamTemplate,
            Icon = x.Icon,
            IsBuiltIn = x.IsBuiltIn
        }).ToPagedListAsync(input.Page, input.PageSize);
        return page;
    }

    /// <summary>
    /// 获取所有数据源类型
    /// </summary>
    [HttpGet]
    public async Task<List<DataSourceTypeDto>> GetList()
        => await _db.Queryable<DataSourceType>().OrderBy(x => x.OrderNo).Select(x => new DataSourceTypeDto
        {
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Version = x.Version,
            AdapterClassName = x.AdapterClassName,
            AssemblyName = x.AssemblyName,
            ParamTemplate = x.ParamTemplate,
            Icon = x.Icon,
            IsBuiltIn = x.IsBuiltIn
        }).ToListAsync();

    /// <summary>
    /// 新增数据源类型
    /// </summary>
    [HttpPost]
    public async Task Add(DataSourceTypeInput input)
    {
        if (await _db.Queryable<DataSourceType>().AnyAsync(x => x.Code == input.Code))
            throw Oops.Oh("类型编码已存在");

        var entity = new DataSourceType
        {
            Code = input.Code,
            Name = input.Name,
            Description = input.Description,
            Version = input.Version ?? "1.0",
            AdapterClassName = input.AdapterClassName ?? "DataProcess.DataSource.Application.Service.Adapter.SqlSugarDataSourceAdapter",
            AssemblyName = input.PluginAssembly ?? typeof(DataSourceTypeService).Assembly.GetName().Name!,
            ParamTemplate = input.ParamTemplateJson,
            Icon = input.Icon,
            IsBuiltIn = input.IsBuiltIn,
            OrderNo = input.OrderNo,
            Status = input.Status,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新数据源类型
    /// </summary>
    [HttpPost]
    public async Task Update(DataSourceTypeUpdateInput input)
    {
        var entity = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Id == input.Id)
                     ?? throw Oops.Oh("类型不存在");

        if (!string.Equals(entity.Code, input.Code, StringComparison.OrdinalIgnoreCase) &&
            await _db.Queryable<DataSourceType>().AnyAsync(x => x.Code == input.Code))
            throw Oops.Oh("类型编码已存在");

        entity.Code = input.Code;
        entity.Name = input.Name;
        entity.Description = input.Description;
        entity.Version = input.Version ?? entity.Version;
        entity.AdapterClassName = input.AdapterClassName ?? entity.AdapterClassName;
        entity.AssemblyName = input.PluginAssembly ?? entity.AssemblyName;
        entity.ParamTemplate = input.ParamTemplateJson ?? entity.ParamTemplate;
        entity.Icon = input.Icon;
        entity.IsBuiltIn = input.IsBuiltIn;
        entity.OrderNo = input.OrderNo;
        entity.Status = input.Status;
        entity.UpdateTime = DateTime.Now;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除数据源类型
    /// </summary>
    [HttpPost]
    public async Task Delete(BaseIdInput input)
    {
        var ok = await _db.Deleteable<DataSourceType>().Where(x => x.Id == input.Id && x.IsBuiltIn == false).ExecuteCommandAsync();
        if (ok == 0) throw Oops.Oh("内置类型禁止删除或类型不存在");
    }

    /// <summary>
    /// 获取数据源类型详情
    /// </summary>
    [HttpGet]
    public async Task<DataSourceTypeDto> Detail(long id)
        => await _db.Queryable<DataSourceType>().Where(x => x.Id == id).Select(x => new DataSourceTypeDto
        {
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Version = x.Version,
            AdapterClassName = x.AdapterClassName,
            AssemblyName = x.AssemblyName,
            ParamTemplate = x.ParamTemplate,
            Icon = x.Icon,
            IsBuiltIn = x.IsBuiltIn
        }).FirstAsync() ?? throw Oops.Oh("类型不存在");

    /// <summary>
    /// 手工触发：重刷所有内置类型
    /// </summary>
    [HttpPost]
    public async Task<int> RefreshBuiltins()
    {
        var adapterClass = "DataProcess.DataSource.Application.Service.Adapter.SqlSugarDataSourceAdapter";
        var asmName = typeof(DataSourceTypeService).Assembly.GetName().Name!;
        var all = Enum.GetValues(typeof(SqlSugar.DbType)).Cast<SqlSugar.DbType>();
        var order = 1;
        var list = all.Select(v => new DataSourceType
        {
            Code = v.ToString(),
            Name = v.ToString(),
            Description = $"内置 {v} 数据源",
            Version = "1.0",
            AdapterClassName = adapterClass,
            AssemblyName = asmName,
            ParamTemplate = JSON.Serialize(new { ConnectionString = "", DbType = v.ToString() }),
            Icon = "",
            IsBuiltIn = true,
            OrderNo = order++,
            Status = true,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        }).ToList();

        var storage = _db.Storageable(list).WhereColumns(x => x.Code).ToStorage();
        var ins = storage.AsInsertable.ExecuteCommand();
        var upd = storage.AsUpdateable.IgnoreColumns(x => new { x.Id, x.CreateTime }).ExecuteCommand();
        return ins + upd;
    }
}