using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Core;

// 主键Id输入参数
public class BaseIdInput
{
    [Required(ErrorMessage = "Id不能为空")]
    public virtual long Id { get; set; }
}

// 全局分页查询输入参数
public class BasePageInput
{
    // 当前页码（从1开始）
    public virtual int Page { get; set; } = 1;
    // 页容量
    public virtual int PageSize { get; set; } = 20;
    // 排序字段（可选）
    public virtual string? Field { get; set; }
    // 排序方向（asc/desc，可选）
    public virtual string? Order { get; set; }
}