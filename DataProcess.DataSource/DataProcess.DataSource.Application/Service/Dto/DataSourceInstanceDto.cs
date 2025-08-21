using System.ComponentModel.DataAnnotations;
using DataProcess.DataSource.Core;

namespace DataProcess.DataSource.Application.Service.Dto;

public class DataSourceInstanceDto
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long? TypeId { get; set; }
    public string? TypeCode { get; set; }
    public string? TypeName { get; set; }
    public long? ParentId { get; set; }
    public string? OverrideJson { get; set; }
    public string? Parameters { get; set; }
    public string? ConfigJson { get; set; }
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}

public class DataSourceInstanceInput
{
    [Required, MaxLength(64)] public string Code { get; set; } = default!;
    [Required, MaxLength(128)] public string Name { get; set; } = default!;
    public long? TypeId { get; set; }
    public string? TypeCode { get; set; }
    public long? ParentId { get; set; }
    public string? OverrideJson { get; set; }
    public string? Parameters { get; set; }
    public string? ConfigJson { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Remark { get; set; }
}

public class DataSourceInstanceUpdateInput : DataSourceInstanceInput
{
    [Required] public long Id { get; set; }
}

public class DataSourceInstancePageInput : BasePageInput
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? TypeCode { get; set; }
    public bool? Enabled { get; set; }
}