using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Service.Dto;

/// <summary>
/// 基础分页输入
/// </summary>
public class BasePageInput
{
    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 基础ID输入
/// </summary>
public class BaseIdInput
{
    /// <summary>
    /// ID
    /// </summary>
    [Required]
    public long Id { get; set; }
}