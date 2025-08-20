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
    
    /// <summary>
    /// �����ֶ�
    /// </summary>
    public string? OrderBy { get; set; }
    
    /// <summary>
    /// ������ asc/desc
    /// </summary>
    public string OrderDirection { get; set; } = "asc";
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

/// <summary>
/// ͳһ��Ӧ���
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