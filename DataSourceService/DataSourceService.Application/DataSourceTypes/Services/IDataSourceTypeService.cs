namespace DataSourceService.Application.DataSourceTypes.Services;

using DataSourceService.Application.DataSourceTypes.Dtos;

public interface IDataSourceTypeService
{
    Task<List<DataSourceTypeDto>> GetListAsync();
    Task<DataSourceTypeDto?> GetAsync(string code);
    Task CreateAsync(DataSourceTypeDto dto);
    Task UpdateAsync(DataSourceTypeDto dto);
    Task DeleteAsync(string code);
    Task LoadPluginAsync(IFormFile file);
}
