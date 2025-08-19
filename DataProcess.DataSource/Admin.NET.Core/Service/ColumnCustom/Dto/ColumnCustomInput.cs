// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 用户表格列配置基数输入参数
/// </summary>
public class BaseColumnCustomInput
{
    /// <summary>
    /// 表格Id
    /// </summary>
    [Required(ErrorMessage = "表格Id不能为空")]
    public virtual string GridId { get; set; }
}

/// <summary>
/// 获取用户表格列配置输入参数
/// </summary>
public class GetColumnCustomInput : BaseColumnCustomInput
{
}

/// <summary>
/// 保存用户表格列配置输入参数
/// </summary>
public class StoreColumnCustomInput
{
    /// <summary>
    /// 表格Id
    /// </summary>
    [Required(ErrorMessage = "表格Id不能为空")]
    public virtual string GridId { get; set; }

    /// <summary>
    /// 冻结列状态数据
    /// </summary>
    public virtual Dictionary<string, string>? FixedData { get; set; }

    /// <summary>
    /// 列宽状态数据
    /// </summary>
    public virtual Dictionary<string, int>? ResizableData { get; set; }

    /// <summary>
    /// 列顺序数据
    /// </summary>
    public virtual Dictionary<string, int>? SortData { get; set; }

    /// <summary>
    /// 显示/隐藏列状态数据
    /// </summary>
    public virtual Dictionary<string, bool>? VisibleData { get; set; }
}

/// <summary>
/// 重置用户表格列配置输入参数
/// </summary>
public class ResetColumnCustomInput : BaseColumnCustomInput
{
}