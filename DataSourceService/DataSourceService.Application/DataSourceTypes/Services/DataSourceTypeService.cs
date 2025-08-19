namespace DataSourceService.Application.DataSourceTypes.Services;

using Common.Interfaces;
using DataSourceService.Application.DataSourceTypes.Dtos;
using DataSourceService.Core.Entities;
using DataSourceService.Core.Repositories;

public class DataSourceTypeService : IDataSourceTypeService, ITransient
{
    private readonly IDataSourceTypeRepo _repo;
    private readonly DataSourcePluginManager _pluginManager;

    public DataSourceTypeService(IDataSourceTypeRepo repo, DataSourcePluginManager pluginManager)
    {
        _repo = repo;
        _pluginManager = pluginManager;
    }

    public async Task<List<DataSourceTypeDto>> GetListAsync()
    {
        var list = await _repo.GetListAsync();
        return list.Adapt<List<DataSourceTypeDto>>();
    }

    public async Task<DataSourceTypeDto?> GetAsync(string code)
    {
        var entity = await _repo.GetAsync(code);
        return entity?.Adapt<DataSourceTypeDto>();
    }

    public async Task CreateAsync(DataSourceTypeDto dto)
    {
        var entity = dto.Adapt<DataSourceType>();
        await _repo.InsertAsync(entity);
    }

    public async Task UpdateAsync(DataSourceTypeDto dto)
    {
        var entity = dto.Adapt<DataSourceType>();
        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(string code)
    {
        await _repo.DeleteAsync(code);
        _pluginManager.Unload(code);
    }

    public async Task LoadPluginAsync(IFormFile file)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "plugins", "datasource");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, file.FileName);
        using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }
        await _pluginManager.LoadAsync(filePath);
    }
}
