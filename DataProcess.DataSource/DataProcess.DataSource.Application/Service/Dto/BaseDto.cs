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
    
    /// <summary>
    /// 排序字段
    /// </summary>
    public string? OrderBy { get; set; }
    
    /// <summary>
    /// 排序方向 asc/desc
    /// </summary>
    public string OrderDirection { get; set; } = "asc";
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

/// <summary>
/// 统一响应结果
/// </summary>
public class ApiResponse<T>
{
    public int Code { get; set; } = 0;
    public string Message { get; set; } = "success";
    public T? Data { get; set; }
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static ApiResponse<T> Success(T data, string message = "success")
    {
        return new ApiResponse<T> { Data = data, Message = message };
    }

    public static ApiResponse<T> Fail(string message, int code = 500)
    {
        return new ApiResponse<T> { Code = code, Message = message };
    }
}