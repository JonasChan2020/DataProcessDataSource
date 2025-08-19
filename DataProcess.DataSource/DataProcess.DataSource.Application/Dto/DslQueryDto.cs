using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Dto;

/// <summary>
/// DSL ²éÑ¯²ÎÊý
/// </summary>
public class DslQueryDto
{
    [Required]
    public long InstanceId { get; set; }

    [Required]
    public string Dsl { get; set; }
}