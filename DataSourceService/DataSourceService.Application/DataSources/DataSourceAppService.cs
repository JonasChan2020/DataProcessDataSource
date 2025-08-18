namespace DataSourceService.Application.DataSources;

using DataSourceService.Application.DataSources.Dtos;
using DataSourceService.Application.DataSources.Services;

public class DataSourceAppService : IDynamicApiController
{
    private readonly IDataSourceService _service;
    public DataSourceAppService(IDataSourceService service)
    {
        _service = service;
    }

    public Task<List<DataSourceDto>> GetList(string? typeCode)
        => _service.GetListAsync(typeCode);

    public Task<DataSourceDto?> Get(string code)
        => _service.GetAsync(code);

    public Task Create(DataSourceDto dto)
        => _service.CreateAsync(dto);

    public Task Update(DataSourceDto dto)
        => _service.UpdateAsync(dto);

    public Task Delete(string code)
        => _service.DeleteAsync(code);

    public Task<bool> TestConnection(DataSourceDto dto)
        => _service.TestConnectionAsync(dto);
}
