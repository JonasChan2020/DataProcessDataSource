// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Newtonsoft.Json;

namespace Admin.NET.Core;

/// <summary>
/// 报表布局配置
/// </summary>
public class SysReportLayoutConfig
{
    /// <summary>
    /// 报表字段集合
    /// </summary>
    public List<SysReportField> Fields { get; set; }

    /// <summary>
    /// 报表参数集合
    /// </summary>
    public List<SysReportParam> Params { get; set; }
}

/// <summary>
/// 报表字段
/// </summary>
public class SysReportField
{
    /// <summary>
    /// 字段名
    /// </summary>
    [JsonConverter(typeof(CamelCaseValueConverter))]
    public string FieldName { get; set; }

    /// <summary>
    /// 字段标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 是否合计
    /// </summary>
    public bool IsSummary { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// 分组标题
    /// </summary>
    public string GroupTitle { get; set; }

    /// <summary>
    /// 列宽度
    /// </summary>
    public int Width { get; set; }
}

/// <summary>
/// 报表参数
/// </summary>
public class SysReportParam
{
    /// <summary>
    /// 参数名
    /// </summary>
    [JsonConverter(typeof(CamelCaseValueConverter))]
    public string ParamName { get; set; }

    /// <summary>
    /// 参数标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 输入控件类型
    /// </summary>
    public string InputCtrl { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public object DefaultValue { get; set; }
}