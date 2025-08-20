using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Service.Dto;

/// <summary>
/// ������ҳ����
/// </summary>
public class BasePageInput
{
    /// <summary>
    /// ҳ��
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// ҳ��С
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// ����ID����
/// </summary>
public class BaseIdInput
{
    /// <summary>
    /// ID
    /// </summary>
    [Required]
    public long Id { get; set; }
}