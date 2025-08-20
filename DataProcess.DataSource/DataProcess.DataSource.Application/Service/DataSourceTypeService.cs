using Furion.DynamicApiController;
using Microsoft.AspNetCore.Http;
using SqlSugar;
using System.IO.Compression;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// 数据源类型管理服务
/// </summary>
[ApiDescriptionSettings(Order = 100, Name = "数据源类型管理")]
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
    /// 获取数据源类型列表
    /// </summary>
    [HttpGet]
    public async Task<List<DataSourceTypeDto>> GetList()
    {
        var list = await _db.Queryable<DataSourceType>()
            .Where(t => t.Status == true)
            .OrderBy(t => t.OrderNo)
            .ThenBy(t => t.CreateTime)
            .ToListAsync();
            
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
    /// 上传插件ZIP包
    /// </summary>
    [HttpPost]
    public async Task<string> UploadPlugin(IFormFile zipFile)
    {
        if (zipFile == null || zipFile.Length == 0)
            throw Oops.Oh("请选择ZIP文件");

        if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh("只支持ZIP格式文件");

        var pluginName = Path.GetFileNameWithoutExtension(zipFile.FileName);
        
        using var stream = zipFile.OpenReadStream();
        var success = await _pluginManager.InstallPluginAsync(stream, pluginName);
        if (!success)
            throw Oops.Oh("插件安装失败");

        var pluginInfo = await _pluginManager.GetPluginInfoAsync(pluginName);
        if (pluginInfo == null)
            throw Oops.Oh("插件配置文件不存在或格式错误");

        var exist = await _db.Queryable<DataSourceType>()
            .Where(t => t.Code == pluginInfo.Code)
            .FirstAsync();

        if (exist != null)
        {
            if (exist.IsBuiltIn)
                throw Oops.Oh("不能覆盖内置类型");

            exist.Name = pluginInfo.Name;
            exist.Description = pluginInfo.Description;
            exist.PluginAssembly = pluginName;
            exist.AdapterClassName = pluginInfo.AdapterClassName;
            exist.ParamTemplateJson = pluginInfo.ParamTemplate;
            exist.Icon = pluginInfo.Icon;
            exist.Version = pluginInfo.Version;
            exist.UpdateTime = DateTime.Now;
            await _db.Updateable(exist).ExecuteCommandAsync();
        }
        else
        {
            var newType = new DataSourceType
            {
                Name = pluginInfo.Name,
                Code = pluginInfo.Code,
                Description = pluginInfo.Description,
                PluginAssembly = pluginName,
                AdapterClassName = pluginInfo.AdapterClassName,
                ParamTemplateJson = pluginInfo.ParamTemplate,
                Icon = pluginInfo.Icon,
                Version = pluginInfo.Version,
                IsBuiltIn = false,
                Status = true,
                OrderNo = 200,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(newType).ExecuteCommandAsync();
        }

        return "插件上传成功";
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    [HttpPost]
    public async Task UnloadPlugin(BaseIdInput input)
    {
        var type = await _db.Queryable<DataSourceType>()
            .Where(t => t.Id == input.Id)
            .FirstAsync();
        if (type == null)
            throw Oops.Oh("数据源类型不存在");

        if (type.IsBuiltIn)
            throw Oops.Oh("内置类型无法卸载");

        var hasInstances = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.TypeId == input.Id)
            .AnyAsync();
        if (hasInstances)
            throw Oops.Oh("该类型下存在数据源实例，无法卸载");

        if (!string.IsNullOrEmpty(type.PluginAssembly))
        {
            _pluginManager.UnloadPlugin(type.PluginAssembly);
            
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "datasource", type.PluginAssembly);
            if (Directory.Exists(pluginDir))
                Directory.Delete(pluginDir, true);
        }

        await _db.Deleteable<DataSourceType>().In(input.Id).ExecuteCommandAsync();
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