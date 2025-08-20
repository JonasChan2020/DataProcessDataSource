using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Service.Dto;

/// <summary>
/// ����Դ�������
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
/// ����Դ��������
/// </summary>
public class DataSourceTypeInput
{
    [Required(ErrorMessage = "�������Ʋ���Ϊ��")]
    [MaxLength(64, ErrorMessage = "�������Ƴ��Ȳ��ܳ���64")]
    public string Name { get; set; }

    [Required(ErrorMessage = "���ͱ��벻��Ϊ��")]
    [MaxLength(64, ErrorMessage = "���ͱ��볤�Ȳ��ܳ���64")]
    public string Code { get; set; }

    [MaxLength(256, ErrorMessage = "����˵�����Ȳ��ܳ���256")]
    public string? Description { get; set; }

    [MaxLength(128, ErrorMessage = "������������Ȳ��ܳ���128")]
    public string? PluginAssembly { get; set; }

    [MaxLength(256, ErrorMessage = "�������������Ȳ��ܳ���256")]
    public string? AdapterClassName { get; set; }

    public string? ParamTemplateJson { get; set; }

    public bool IsBuiltIn { get; set; }

    public int OrderNo { get; set; } = 100;

    public bool Status { get; set; } = true;

    [MaxLength(128, ErrorMessage = "ͼ�곤�Ȳ��ܳ���128")]
    public string? Icon { get; set; }

    [MaxLength(32, ErrorMessage = "�汾�ų��Ȳ��ܳ���32")]
    public string? Version { get; set; }
}

/// <summary>
/// ����Դ���ͷ�ҳ��ѯ
/// </summary>
public class DataSourceTypePageInput : BasePageInput
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? IsBuiltIn { get; set; }
    public bool? Status { get; set; }
}

/// <summary>
/// ����Դ���͸�������
/// </summary>
public class DataSourceTypeUpdateInput : DataSourceTypeInput
{
    [Required]
    public long Id { get; set; }
}