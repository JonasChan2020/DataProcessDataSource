namespace DataProcess.DataSource.Application.Dto;

/// <summary>
/// 统一API响应模型
/// </summary>
public class ApiResponse<T>
{
    public int Code { get; set; } = 0;
    public string Msg { get; set; } = "success";
    public T Data { get; set; }

    public static ApiResponse<T> Success(T data) => new ApiResponse<T> { Data = data };
    public static ApiResponse<T> Fail(string msg, int code = 500) => new ApiResponse<T> { Code = code, Msg = msg };
}