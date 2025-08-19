using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Dto;

/// <summary>
/// 新建数据源实例参数
/// </summary>
public class DataSourceInstanceCreateDto
{
    [Required(ErrorMessage = "实例名称不能为空")]
    [MaxLength(64, ErrorMessage = "实例名称不能超过64字符")]
    public string Name { get; set; }

    [Required(ErrorMessage = "类型Id不能为空")]
    public long TypeId { get; set; }

    [Required(ErrorMessage = "配置参数不能为空")]
    public string ConfigJson { get; set; }

    public long? ParentId { get; set; }
}