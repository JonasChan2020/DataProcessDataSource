using Furion.DynamicApiController;
using SqlSugar;
using System.Diagnostics;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;
using DataProcess.DataSource.Application.Service.Adapter;
using DataProcess.DataSource.Core.Plugin;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// ����Դʵ������
/// </summary>
[ApiDescriptionSettings("����Դ", Order = 2)]
public class DataSourceInstanceService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly PluginManager _pluginManager;

    public DataSourceInstanceService(ISqlSugarClient db, PluginManager pluginManager)
    {
        _db = db;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// ��������Դʵ��
    /// </summary>
    public async Task<long> Add([FromBody] DataSourceInstanceDto dto)
    {
        var entity = dto.Adapt<DataSourceInstance>();
        entity.Id = 0;
        var id = await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
        return id;
    }

    /// <summary>
    /// �༭����Դʵ��
    /// </summary>
    public async Task<bool> Update([FromBody] DataSourceInstanceDto dto)
    {
        var entity = dto.Adapt<DataSourceInstance>();
        var ok = await _db.Updateable(entity).IgnoreColumns(it => new { it.Id }).ExecuteCommandAsync();
        return ok > 0;
    }

    /// <summary>
    /// ɾ������Դʵ��
    /// </summary>
    public async Task<bool> Delete([FromBody] long id)
    {
        var ok = await _db.Deleteable<DataSourceInstance>().In(id).ExecuteCommandAsync();
        return ok > 0;
    }

    /// <summary>
    /// ��ȡ��������Դʵ��
    /// </summary>
    public async Task<List<DataSourceInstanceDto>> GetListAsync()
    {
        var list = await _db.Queryable<DataSourceInstance>().ToListAsync();
        return list.Adapt<List<DataSourceInstanceDto>>();
    }

    /// <summary>
    /// ��������
    /// </summary>
    public async Task<bool> TestConnection([FromBody] long id)
    {
        var instance = await _db.Queryable<DataSourceInstance>().InSingleAsync(id);
        if (instance == null) throw Oops.Oh("ʵ��������");
        var type = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Id == instance.TypeId);
        if (type == null) throw Oops.Oh("���Ͳ�����");

        var adapter = _pluginManager.GetAdapter(type.PluginAssembly!, type.AdapterClassName!);
        if (adapter == null) throw Oops.Oh("����������ʧ��");

        return await adapter.TestConnectionAsync(instance.ConfigJson);
    }
}