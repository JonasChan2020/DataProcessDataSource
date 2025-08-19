namespace DataSourceService.Application.DataSources.Services;

using System.Text.Json;
using Common.Models;
using DataSourceService.Application.DataSources.Dtos;
using DataSourceService.Core.Entities;
using DataSourceService.Core.Repositories;

public class DataSourceService : IDataSourceService, ITransient
{
    private readonly IDataSourceRepo _repo;
    private readonly DataSourcePluginManager _pluginManager;

    public DataSourceService(IDataSourceRepo repo, DataSourcePluginManager pluginManager)
    {
        _repo = repo;
        _pluginManager = pluginManager;
    }

    public async Task<List<DataSourceDto>> GetListAsync(string? typeCode)
    {
        var list = await _repo.GetListAsync(typeCode);
        return list.Adapt<List<DataSourceDto>>();
    }

    public async Task<DataSourceDto?> GetAsync(string code)
    {
        var entity = await _repo.GetAsync(code);
        return entity?.Adapt<DataSourceDto>();
    }

    public async Task CreateAsync(DataSourceDto dto)
    {
        var entity = dto.Adapt<DataSource>();
        await _repo.InsertAsync(entity);
    }

    public async Task UpdateAsync(DataSourceDto dto)
    {
        var entity = dto.Adapt<DataSource>();
        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(string code)
    {
        await _repo.DeleteAsync(code);
    }

    public async Task<bool> TestConnectionAsync(DataSourceDto dto)
    {
        if (string.IsNullOrEmpty(dto.Config)) return false;
        var config = JsonSerializer.Deserialize<DataSourceConfig>(dto.Config);
        var adapter = _pluginManager.GetAdapter(dto.TypeCode);
        if (adapter == null || config == null) return false;
        return await adapter.TestConnectionAsync(config);
    }
}
