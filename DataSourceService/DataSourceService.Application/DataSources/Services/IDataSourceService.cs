namespace DataSourceService.Application.DataSources.Services;

using DataSourceService.Application.DataSources.Dtos;

public interface IDataSourceService
{
    Task<List<DataSourceDto>> GetListAsync(string? typeCode);
    Task<DataSourceDto?> GetAsync(string code);
    Task CreateAsync(DataSourceDto dto);
    Task UpdateAsync(DataSourceDto dto);
    Task DeleteAsync(string code);
    Task<bool> TestConnectionAsync(DataSourceDto dto);
}
