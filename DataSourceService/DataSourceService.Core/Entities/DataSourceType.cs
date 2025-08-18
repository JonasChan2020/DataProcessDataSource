namespace DataSourceService.Core.Entities;

using SqlSugar;
using DataSourceService.Core.Attributes;

[IncreTable]
[SugarTable("datasource_types")]
public class DataSourceType
{
    [SugarColumn(IsPrimaryKey = true)]
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ConfigSchema { get; set; }
    public string? PluginAssembly { get; set; }
}
