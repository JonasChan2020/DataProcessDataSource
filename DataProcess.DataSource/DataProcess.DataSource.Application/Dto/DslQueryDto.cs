using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Dto;

/// <summary>
/// DSL ��ѯ����
/// </summary>
public class DslQueryDto
{
    [Required]
    public long InstanceId { get; set; }

    [Required]
    public string Dsl { get; set; }
}