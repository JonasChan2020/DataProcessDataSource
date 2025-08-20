using Furion.DynamicApiController;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.IO.Compression;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapster;
using DataProcess.DataSource.Core.Paging;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// ����Դ���ͷ���
/// </summary>
[ApiDescriptionSettings("����Դ", Order = 1)]
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
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), t => t.Name.Contains(input.Name!))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), t => t.Code.Contains(input.Code!))
            .WhereIF(input.IsBuiltIn.HasValue, t => t.IsBuiltIn == input.IsBuiltIn)
            .WhereIF(input.Status.HasValue, t => t.Status == input.Status)
            // SqlSugar ��֧�� ThenBy������һ���Դ����������ֶ�
            .OrderBy($"{nameof(DataSourceType.OrderNo)} asc, {nameof(DataSourceType.CreateTime)} asc");

        var result = await query.ToPagedListAsync(input.Page, input.PageSize);

        return new SqlSugarPagedList<DataSourceTypeDto>
        {
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total,
            TotalPages = result.TotalPages,
            HasNextPage = result.HasNextPage,
            HasPrevPage = result.HasPrevPage,
            Items = result.Items.Adapt<List<DataSourceTypeDto>>()
        };
    }

    /// <summary>
    /// ��ȡ��������Դ����
    /// </summary>
    public async Task<List<DataSourceTypeDto>> GetListAsync()
    {
        var list = await _db.Queryable<DataSourceType>().ToListAsync();
        return list.Adapt<List<DataSourceTypeDto>>();
    }

    /// <summary>
    /// ��������Դ����
    /// </summary>
    [HttpPost]
    public async Task<long> Add(DataSourceTypeInput input)
    {
        var exist = await _db.Queryable<DataSourceType>().AnyAsync(t => t.Code == input.Code);
        if (exist) throw Oops.Oh("���ͱ����Ѵ���");

        var entity = input.Adapt<DataSourceType>();
        entity.CreateTime = DateTime.Now;
        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    /// <summary>
    /// ��������Դ����
    /// </summary>
    [HttpPost]
    public async Task Update(DataSourceTypeUpdateInput input)
    {
        var exist = await _db.Queryable<DataSourceType>().AnyAsync(t => t.Code == input.Code && t.Id != input.Id);
        if (exist) throw Oops.Oh("���ͱ����Ѵ���");

        var entity = await _db.Queryable<DataSourceType>().FirstAsync(t => t.Id == input.Id);
        if (entity == null) throw Oops.Oh("����Դ���Ͳ�����");
        if (entity.IsBuiltIn) throw Oops.Oh("�������Ͳ������޸�");

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
        var entity = await _db.Queryable<DataSourceType>().FirstAsync(t => t.Id == input.Id);
        if (entity == null) return;
        if (entity.IsBuiltIn) throw Oops.Oh("�������Ͳ�����ɾ��");

        var hasInstances = await _db.Queryable<DataSourceInstance>().AnyAsync(i => i.TypeId == input.Id);
        if (hasInstances) throw Oops.Oh("�������´�������Դʵ�����޷�ɾ��");

        if (!string.IsNullOrEmpty(entity.AssemblyName))
        {
            _pluginManager.UnloadPlugin(entity.AssemblyName!);
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "datasource", entity.AssemblyName!);
            if (Directory.Exists(pluginDir)) Directory.Delete(pluginDir, true);
        }

        await _db.Deleteable<DataSourceType>().In(input.Id).ExecuteCommandAsync();
    }

    /// <summary>
    /// ��ȡ����Դ��������
    /// </summary>
    [HttpGet]
    public async Task<DataSourceTypeDto> GetDetail(long id)
    {
        var entity = await _db.Queryable<DataSourceType>().FirstAsync(t => t.Id == id);
        if (entity == null) throw Oops.Oh("����Դ���Ͳ�����");
        return entity.Adapt<DataSourceTypeDto>();
    }

    /// <summary>
    /// �ϴ������ZIP��
    /// </summary>
    public async Task<bool> UploadPlugin([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0) throw Oops.Oh("�ļ�����Ϊ��");
        var pluginName = Path.GetFileNameWithoutExtension(file.FileName);
        using var stream = file.OpenReadStream();
        var ok = await _pluginManager.InstallPluginAsync(stream, pluginName);
        if (!ok) throw Oops.Oh("�����װʧ��");

        var info = await _pluginManager.GetPluginInfoAsync(pluginName);
        if (info == null) throw Oops.Oh("plugin.json ȱʧ���ʽ����");

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
            IsBuiltIn = false,
            UpdateTime = DateTime.Now
        };
        await _db.Storageable(type).ExecuteCommandAsync();
        return true;
    }

    /// <summary>
    /// ж�ز��
    /// </summary>
    public async Task<bool> UninstallPlugin([FromBody] string code)
    {
        var type = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Code == code);
        if (type == null || type.IsBuiltIn) throw Oops.Oh("���Ͳ����ڻ�Ϊ��������");
        _pluginManager.UnloadPlugin(type.AssemblyName);
        await _db.Deleteable<DataSourceType>().Where(x => x.Code == code).ExecuteCommandAsync();
        return true;
    }

    /// <summary>
    /// ��������Դ����
    /// </summary>
    [HttpPost]
    public async Task Import(IFormFile file)
    {
        if (file == null || file.Length == 0) throw Oops.Oh("��ѡ�����ļ�");

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var types = JSON.Deserialize<List<DataSourceTypeInput>>(json);
        if (types == null || !types.Any()) throw Oops.Oh("�����ļ���ʽ�����������");

        var insertList = new List<DataSourceType>();
        foreach (var t in types)
        {
            var exist = await _db.Queryable<DataSourceType>().AnyAsync(x => x.Code == t.Code);
            if (!exist)
            {
                var entity = t.Adapt<DataSourceType>();
                entity.CreateTime = DateTime.Now;
                insertList.Add(entity);
            }
        }
        if (insertList.Any()) await _db.Insertable(insertList).ExecuteCommandAsync();
    }

    /// <summary>
    /// ��������Դ����
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var list = await _db.Queryable<DataSourceType>().Where(t => !t.IsBuiltIn).ToListAsync();
        var exportData = list.Adapt<List<DataSourceTypeInput>>();
        var json = JSON.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return new FileContentResult(bytes, "application/json")
        {
            FileDownloadName = $"datasource_types_{DateTime.Now:yyyyMMddHHmmss}.json"
        };
    }
}