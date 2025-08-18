namespace DataSourceService.Core.Entities;

using SqlSugar;
using DataSourceService.Core.Attributes;

[IncreTable]
[SugarTable("datasources")]
public class DataSource
{
    [SugarColumn(IsPrimaryKey = true)]
    public string Code { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Config { get; set; }
    public bool Enabled { get; set; } = true;
}
