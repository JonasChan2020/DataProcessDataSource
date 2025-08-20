using Furion.DynamicApiController;
using SqlSugar;
using System.Diagnostics;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.Service.Dto;
using DataProcess.DataSource.Application.Service.Plugin;
using DataProcess.DataSource.Application.Service.Adapter;
using DataProcess.DataSource.Core.Plugin;
using DataProcess.DataSource.Core.Entity;
using Microsoft.AspNetCore.Mvc;
using Furion.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// 数据源实例服务
/// </summary>
[ApiDescriptionSettings("数据源", Order = 2)]
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
    /// 新增数据源实例
    /// </summary>
    public async Task<long> Add([FromBody] DataSourceInstanceDto dto)
    {
        var entity = dto.Adapt<DataSourceInstance>();
        entity.Id = 0;
        var id = await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
        return id;
    }

    /// <summary>
    /// 编辑数据源实例
    /// </summary>
    public async Task<bool> Update([FromBody] DataSourceInstanceDto dto)
    {
        var entity = dto.Adapt<DataSourceInstance>();
        var ok = await _db.Updateable(entity).IgnoreColumns(it => new { it.Id }).ExecuteCommandAsync();
        return ok > 0;
    }

    /// <summary>
    /// 删除数据源实例
    /// </summary>
    public async Task<bool> Delete([FromBody] long id)
    {
        var ok = await _db.Deleteable<DataSourceInstance>().In(id).ExecuteCommandAsync();
        return ok > 0;
    }

    /// <summary>
    /// 获取所有数据源实例
    /// </summary>
    public async Task<List<DataSourceInstanceDto>> GetListAsync()
    {
        var list = await _db.Queryable<DataSourceInstance>().ToListAsync();
        return list.Adapt<List<DataSourceInstanceDto>>();
    }

    /// <summary>
    /// 测试连接
    /// </summary>
    public async Task<bool> TestConnection([FromBody] long id)
    {
        var instance = await _db.Queryable<DataSourceInstance>().InSingleAsync(id);
        if (instance == null) throw Oops.Oh("实例不存在");
        var type = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Code == instance.TypeCode);
        if (type == null) throw Oops.Oh("类型不存在");

        var adapter = _pluginManager.GetAdapter(type.AssemblyName, type.AdapterClassName);
        if (adapter == null) throw Oops.Oh("适配器加载失败");

        return await adapter.TestConnectionAsync(instance.ConfigJson);
    }
}