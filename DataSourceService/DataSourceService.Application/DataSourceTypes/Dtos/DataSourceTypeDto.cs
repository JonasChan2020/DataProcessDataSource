namespace DataSourceService.Application.DataSourceTypes.Dtos;

public class DataSourceTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ConfigSchema { get; set; }
    public string? PluginAssembly { get; set; }
}
