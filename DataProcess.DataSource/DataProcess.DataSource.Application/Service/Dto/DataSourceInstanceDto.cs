using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Service.Dto;

/// <summary>
/// 数据源实例输出
/// </summary>
public class DataSourceInstanceDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string TypeCode { get; set; } = default!;
    public string ConfigJson { get; set; } = default!;
    public string? Description { get; set; }
    public long? ParentId { get; set; }
}

/// <summary>
/// 数据源实例输入
/// </summary>
public class DataSourceInstanceInput
{
    [Required(ErrorMessage = "实例名称不能为空")]
    [MaxLength(64, ErrorMessage = "实例名称长度不能超过64")]
    public string Name { get; set; }

    [Required(ErrorMessage = "实例编码不能为空")]
    [MaxLength(64, ErrorMessage = "实例编码长度不能超过64")]
    public string Code { get; set; }

    [Required(ErrorMessage = "类型Id不能为空")]
    public long TypeId { get; set; }

    [Required(ErrorMessage = "配置参数不能为空")]
    public string ConfigJson { get; set; }

    public long? ParentId { get; set; }

    public bool Status { get; set; } = true;

    [MaxLength(512, ErrorMessage = "备注长度不能超过512")]
    public string? Remark { get; set; }

    public int OrderNo { get; set; } = 100;
}

/// <summary>
/// 数据源实例分页查询
/// </summary>
public class DataSourceInstancePageInput : BasePageInput
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public long? TypeId { get; set; }
    public long? ParentId { get; set; }
    public bool? Status { get; set; }
}

/// <summary>
/// 数据源实例更新输入
/// </summary>
public class DataSourceInstanceUpdateInput : DataSourceInstanceInput
{
    [Required]
    public long Id { get; set; }
}

/// <summary>
/// 连接测试输入
/// </summary>
public class TestConnectionInput
{
    public long? InstanceId { get; set; }
    public long? TypeId { get; set; }
    public string? ConfigJson { get; set; }
}

/// <summary>
/// 连接测试结果
/// </summary>
public class TestConnectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public long ResponseTime { get; set; }
    public string? ErrorDetail { get; set; }
}