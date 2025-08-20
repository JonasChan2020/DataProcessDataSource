using Furion.DynamicApiController;
using Microsoft.AspNetCore.Http;
using SqlSugar;
using System.IO.Compression;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// ����Դ���͹������
/// </summary>
[ApiDescriptionSettings(Order = 100, Name = "����Դ���͹���")]
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
    /// ��ҳ��ѯ����Դ����
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
    /// ��ȡ����Դ�����б�
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
    /// ��������Դ����
    /// </summary>
    [HttpPost]
    public async Task<long> Add(DataSourceTypeInput input)
    {
        var exist = await _db.Queryable<DataSourceType>()
            .Where(t => t.Code == input.Code)
            .AnyAsync();
        if (exist)
            throw Oops.Oh("���ͱ����Ѵ���");

        var entity = input.Adapt<DataSourceType>();
        entity.CreateTime = DateTime.Now;
        var id = await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
        return id;
    }

    /// <summary>
    /// ��������Դ����
    /// </summary>
    [HttpPost]
    public async Task Update(DataSourceTypeUpdateInput input)
    {
        var exist = await _db.Queryable<DataSourceType>()
            .Where(t => t.Code == input.Code && t.Id != input.Id)
            .AnyAsync();
        if (exist)
            throw Oops.Oh("���ͱ����Ѵ���");

        var entity = await _db.Queryable<DataSourceType>()
            .Where(t => t.Id == input.Id)
            .FirstAsync();
        if (entity == null)
            throw Oops.Oh("����Դ���Ͳ�����");

        if (entity.IsBuiltIn)
            throw Oops.Oh("�������Ͳ������޸�");

        entity = input.Adapt(entity);
        entity.UpdateTime = DateTime.Now;
        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// ɾ������Դ����
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
            throw Oops.Oh("�������Ͳ�����ɾ��");

        var hasInstances = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.TypeId == input.Id)
            .AnyAsync();
        if (hasInstances)
            throw Oops.Oh("�������´�������Դʵ�����޷�ɾ��");

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
    /// ��ȡ����Դ��������
    /// </summary>
    [HttpGet]
    public async Task<DataSourceTypeDto> GetDetail(long id)
    {
        var entity = await _db.Queryable<DataSourceType>()
            .Where(t => t.Id == id)
            .FirstAsync();
        if (entity == null)
            throw Oops.Oh("����Դ���Ͳ�����");

        return entity.Adapt<DataSourceTypeDto>();
    }

    /// <summary>
    /// �ϴ����ZIP��
    /// </summary>
    [HttpPost]
    public async Task<string> UploadPlugin(IFormFile zipFile)
    {
        if (zipFile == null || zipFile.Length == 0)
            throw Oops.Oh("��ѡ��ZIP�ļ�");

        if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh("ֻ֧��ZIP��ʽ�ļ�");

        var pluginName = Path.GetFileNameWithoutExtension(zipFile.FileName);
        
        using var stream = zipFile.OpenReadStream();
        var success = await _pluginManager.InstallPluginAsync(stream, pluginName);
        if (!success)
            throw Oops.Oh("�����װʧ��");

        var pluginInfo = await _pluginManager.GetPluginInfoAsync(pluginName);
        if (pluginInfo == null)
            throw Oops.Oh("��������ļ������ڻ��ʽ����");

        var exist = await _db.Queryable<DataSourceType>()
            .Where(t => t.Code == pluginInfo.Code)
            .FirstAsync();

        if (exist != null)
        {
            if (exist.IsBuiltIn)
                throw Oops.Oh("���ܸ�����������");

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

        return "����ϴ��ɹ�";
    }

    /// <summary>
    /// ж�ز��
    /// </summary>
    [HttpPost]
    public async Task UnloadPlugin(BaseIdInput input)
    {
        var type = await _db.Queryable<DataSourceType>()
            .Where(t => t.Id == input.Id)
            .FirstAsync();
        if (type == null)
            throw Oops.Oh("����Դ���Ͳ�����");

        if (type.IsBuiltIn)
            throw Oops.Oh("���������޷�ж��");

        var hasInstances = await _db.Queryable<DataSourceInstance>()
            .Where(i => i.TypeId == input.Id)
            .AnyAsync();
        if (hasInstances)
            throw Oops.Oh("�������´�������Դʵ�����޷�ж��");

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
    /// ��������Դ����
    /// </summary>
    [HttpPost]
    public async Task Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw Oops.Oh("��ѡ�����ļ�");

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        
        var types = JSON.Deserialize<List<DataSourceTypeInput>>(json);
        if (types == null || !types.Any())
            throw Oops.Oh("�����ļ���ʽ�����������");

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
    /// ��������Դ����
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