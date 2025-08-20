using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Service.Dto;

/// <summary>
/// 数据源类型输出
/// </summary>
public class DataSourceTypeDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string AdapterClassName { get; set; } = default!;
    public string AssemblyName { get; set; } = default!;
    public string? ParamTemplate { get; set; }
    public string? Icon { get; set; }
    public bool IsBuiltIn { get; set; }
}

/// <summary>
/// 数据源类型输入
/// </summary>
public class DataSourceTypeInput
{
    [Required(ErrorMessage = "类型名称不能为空")]
    [MaxLength(64, ErrorMessage = "类型名称长度不能超过64")]
    public string Name { get; set; }

    [Required(ErrorMessage = "类型编码不能为空")]
    [MaxLength(64, ErrorMessage = "类型编码长度不能超过64")]
    public string Code { get; set; }

    [MaxLength(256, ErrorMessage = "类型说明长度不能超过256")]
    public string? Description { get; set; }

    [MaxLength(128, ErrorMessage = "插件程序集名长度不能超过128")]
    public string? PluginAssembly { get; set; }

    [MaxLength(256, ErrorMessage = "适配器类名长度不能超过256")]
    public string? AdapterClassName { get; set; }

    public string? ParamTemplateJson { get; set; }

    public bool IsBuiltIn { get; set; }

    public int OrderNo { get; set; } = 100;

    public bool Status { get; set; } = true;

    [MaxLength(128, ErrorMessage = "图标长度不能超过128")]
    public string? Icon { get; set; }

    [MaxLength(32, ErrorMessage = "版本号长度不能超过32")]
    public string? Version { get; set; }
}

/// <summary>
/// 数据源类型分页查询
/// </summary>
public class DataSourceTypePageInput : BasePageInput
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? IsBuiltIn { get; set; }
    public bool? Status { get; set; }
}

/// <summary>
/// 数据源类型更新输入
/// </summary>
public class DataSourceTypeUpdateInput : DataSourceTypeInput
{
    [Required]
    public long Id { get; set; }
}