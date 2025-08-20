using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Service.Dto;

/// <summary>
/// ����Դʵ�����
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
/// ����Դʵ������
/// </summary>
public class DataSourceInstanceInput
{
    [Required(ErrorMessage = "ʵ�����Ʋ���Ϊ��")]
    [MaxLength(64, ErrorMessage = "ʵ�����Ƴ��Ȳ��ܳ���64")]
    public string Name { get; set; }

    [Required(ErrorMessage = "ʵ�����벻��Ϊ��")]
    [MaxLength(64, ErrorMessage = "ʵ�����볤�Ȳ��ܳ���64")]
    public string Code { get; set; }

    [Required(ErrorMessage = "����Id����Ϊ��")]
    public long TypeId { get; set; }

    [Required(ErrorMessage = "���ò�������Ϊ��")]
    public string ConfigJson { get; set; }

    public long? ParentId { get; set; }

    public bool Status { get; set; } = true;

    [MaxLength(512, ErrorMessage = "��ע���Ȳ��ܳ���512")]
    public string? Remark { get; set; }

    public int OrderNo { get; set; } = 100;
}

/// <summary>
/// ����Դʵ����ҳ��ѯ
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
/// ����Դʵ����������
/// </summary>
public class DataSourceInstanceUpdateInput : DataSourceInstanceInput
{
    [Required]
    public long Id { get; set; }
}

/// <summary>
/// ���Ӳ�������
/// </summary>
public class TestConnectionInput
{
    public long? InstanceId { get; set; }
    public long? TypeId { get; set; }
    public string? ConfigJson { get; set; }
}

/// <summary>
/// ���Ӳ��Խ��
/// </summary>
public class TestConnectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public long ResponseTime { get; set; }
    public string? ErrorDetail { get; set; }
}