using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Dto;

/// <summary>
/// �½�����Դʵ������
/// </summary>
public class DataSourceInstanceCreateDto
{
    [Required(ErrorMessage = "ʵ�����Ʋ���Ϊ��")]
    [MaxLength(64, ErrorMessage = "ʵ�����Ʋ��ܳ���64�ַ�")]
    public string Name { get; set; }

    [Required(ErrorMessage = "����Id����Ϊ��")]
    public long TypeId { get; set; }

    [Required(ErrorMessage = "���ò�������Ϊ��")]
    public string ConfigJson { get; set; }

    public long? ParentId { get; set; }
}