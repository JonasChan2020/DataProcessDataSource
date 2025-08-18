namespace DataSourceService.Application.DataSourceTypes;

using DataSourceService.Application.DataSourceTypes.Dtos;
using DataSourceService.Application.DataSourceTypes.Services;

public class DataSourceTypeAppService : IDynamicApiController
{
    private readonly IDataSourceTypeService _service;
    public DataSourceTypeAppService(IDataSourceTypeService service)
    {
        _service = service;
    }

    public Task<List<DataSourceTypeDto>> GetList()
        => _service.GetListAsync();

    public Task<DataSourceTypeDto?> Get(string code)
        => _service.GetAsync(code);

    public Task Create(DataSourceTypeDto dto)
        => _service.CreateAsync(dto);

    public Task Update(DataSourceTypeDto dto)
        => _service.UpdateAsync(dto);

    public Task Delete(string code)
        => _service.DeleteAsync(code);

    public Task UploadPlugin([FromForm] IFormFile file)
        => _service.LoadPluginAsync(file);
}
