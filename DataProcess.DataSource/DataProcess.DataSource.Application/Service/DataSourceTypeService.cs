using Furion.DynamicApiController;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.IO.Compression;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;
using Furion.Json;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// 数据源类型服务
/// </summary>
[ApiDescriptionSettings("数据源", Order = 1)]
public class DataSourceTypeService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly PluginManager _pluginManager;

    public DataSourceTypeService(ISqlSugarClient db, PluginManager pluginManager)
    {
        _db = db;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// 分页查询数据源类型
    /// </summary>
    [HttpPost]
    public async Task<SqlSugarPagedList<DataSourceTypeDto>> Page(DataSourceTypePageInput input)
    {
        var query = _db.Queryable<DataSourceType>()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), t => t.Name.Contains(input.Name))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), t => t.Code.Contains(input.Code))
            .WhereIF(input.IsBuiltIn.HasValue, t => t.IsBuiltIn == input.IsBuiltIn)
            .WhereIF(input.Status.HasValue, t => t.Status == input.Status)
            .OrderBy(t => t.OrderNo)
            .ThenBy(t => t.CreateTime);

        var result = await query.ToPagedListAsync(input.Page, input.PageSize);
        return result.Adapt<SqlSugarPagedList<DataSourceTypeDto>>();
    }

    /// <summary>
    /// 获取所有数据源类型
    /// </summary>
    public async Task<List<DataSourceTypeDto>> GetListAsync()
    {
        var list = await _db.Queryable<DataSourceType>().ToListAsync();
        return list.Adapt<List<DataSourceTypeDto>>();
    }

    /// <summary>
    /// 新增数据源类型
    /// </summary>
    [HttpPost]
    public async Task<long> Add(DataSourceTypeInput input)
    {
        var exist = await _db.Queryable<DataSourceType>()
            .Where(t => t.Code == input.Code)
            .AnyAsync();
        if (exist)
            throw Oops.Oh("类型编码已存在");

        var entity = input.Adapt<DataSourceType>();
        entity.CreateTime = DateTime.Now;
        var id = await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
        return id;
    }

    /// <summary>
    /// 更新数据源类型
    /// </summary>
    [HttpPost]
    public async Task Update(DataSourceTypeUpdateInput input)
    {
        var exist = await _db.Queryable<DataSourceType>()
            .Where(t => t.Code == input.Code && t.Id != input.Id)
            .AnyAsync();
        if (exist)
            throw Oops.Oh("类型编码已存在");

        var entity = await _db.Queryable<DataSourceType>()
            .Where(t => t.Id == input.Id)
            .FirstAsync();
        if (entity == null)
            throw Oops.Oh("数据源类型不存在");

        if (entity.IsBuiltIn)
            throw Oops.Oh("内置类型不允许修改");

        entity = input.Adapt(entity);
        entity.UpdateTime = DateTime.Now;
        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除数据源类型
    /// </summary>
    [HttpPost]
    public async Task Delete(BaseIdInput input)
    {
        var entity = await _db.Queryable<DataSourceType>()
            .Where(t => t.Id == input.Id)
            .FirstAsync();
        if (entity == null)
            return;

        if (entity.IsBuiltIn)
            throw Oops.Oh("内置类型不允许删除");

        var hasInstances = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.TypeId == input.Id)
            .AnyAsync();
        if (hasInstances)
            throw Oops.Oh("该类型下存在数据源实例，无法删除");

        if (!string.IsNullOrEmpty(entity.PluginAssembly))
        {
            _pluginManager.UnloadPlugin(entity.PluginAssembly);

            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "datasource", entity.PluginAssembly);
            if (Directory.Exists(pluginDir))
                Directory.Delete(pluginDir, true);
        }

        await _db.Deleteable<DataSourceType>().In(input.Id).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取数据源类型详情
    /// </summary>
    [HttpGet]
    public async Task<DataSourceTypeDto> GetDetail(long id)
    {
        var entity = await _db.Queryable<DataSourceType>()
            .Where(t => t.Id == id)
            .FirstAsync();
        if (entity == null)
            throw Oops.Oh("数据源类型不存在");

        return entity.Adapt<DataSourceTypeDto>();
    }

    /// <summary>
    /// 上传插件（ZIP）
    /// </summary>
    public async Task<bool> UploadPlugin([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0) throw Oops.Oh("文件不能为空");
        var pluginName = Path.GetFileNameWithoutExtension(file.FileName);
        using var stream = file.OpenReadStream();
        var ok = await _pluginManager.InstallPluginAsync(stream, pluginName);
        if (!ok) throw Oops.Oh("插件安装失败");

        // 读取 plugin.json 并注册类型
        var info = await _pluginManager.GetPluginInfoAsync(pluginName);
        if (info == null) throw Oops.Oh("plugin.json 缺失或格式错误");

        var type = new DataSourceType
        {
            Code = info.Code,
            Name = info.Name,
            Description = info.Description,
            Version = info.Version,
            AdapterClassName = info.AdapterClassName,
            AssemblyName = pluginName,
            ParamTemplate = info.ParamTemplate,
            Icon = info.Icon,
            IsBuiltIn = false
        };
        // 覆盖或新增
        await _db.Storageable(type).ExecuteCommandAsync();
        return true;
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    public async Task<bool> UninstallPlugin([FromBody] string code)
    {
        var type = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Code == code);
        if (type == null || type.IsBuiltIn) throw Oops.Oh("类型不存在或为内置类型");
        _pluginManager.UnloadPlugin(type.AssemblyName);
        await _db.Deleteable<DataSourceType>().Where(x => x.Code == code).ExecuteCommandAsync();
        return true;
    }

    /// <summary>
    /// 导入数据源类型
    /// </summary>
    [HttpPost]
    public async Task Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw Oops.Oh("请选择导入文件");

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var types = JSON.Deserialize<List<DataSourceTypeInput>>(json);
        if (types == null || !types.Any())
            throw Oops.Oh("导入文件格式错误或无数据");

        var insertList = new List<DataSourceType>();
        foreach (var type in types)
        {
            var exist = await _db.Queryable<DataSourceType>()
                .Where(t => t.Code == type.Code)
                .AnyAsync();
            if (!exist)
            {
                var entity = type.Adapt<DataSourceType>();
                entity.CreateTime = DateTime.Now;
                insertList.Add(entity);
            }
        }

        if (insertList.Any())
        {
            await _db.Insertable(insertList).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 导出数据源类型
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var list = await _db.Queryable<DataSourceType>()
            .Where(t => !t.IsBuiltIn)
            .ToListAsync();

        var exportData = list.Adapt<List<DataSourceTypeInput>>();
        var json = JSON.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return new FileContentResult(bytes, "application/json")
        {
            FileDownloadName = $"datasource_types_{DateTime.Now:yyyyMMddHHmmss}.json"
        };
    }
}