// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 验证规则选项
/// </summary>
public class VerifyRuleItem
{
    /// <summary>
    /// 编码
    /// </summary>
    public long Key { get; set; }

    /// <summary>
    /// 验证类型
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 验证错误消息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public string Min { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public string Max { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    public string Pattern { get; set; }

    /// <summary>
    /// 数据类型(搭配正则)
    /// </summary>
    public string DataType { get; set; }
}