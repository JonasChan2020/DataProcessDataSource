namespace DataSourceService.Application.DataSources.Dtos;

public class DataSourceDto
{
    public string Code { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Config { get; set; }
    public bool Enabled { get; set; }
}
