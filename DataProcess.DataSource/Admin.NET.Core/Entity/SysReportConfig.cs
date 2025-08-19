// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 系统报表配置表
/// </summary>
[SugarTable(null, "系统报表配置表")]
[SysTable]
public class SysReportConfig : EntityBase
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnDescription = "名称", Length = 254)]
    [MaxLength(254)]
    public string Name { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnDescription = "描述", Length = 512)]
    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// 数据源类型
    /// </summary>
    [SugarColumn(ColumnDescription = "数据源类型")]
    public ReportConfigDsTypeEnum DsType { get; set; }

    /// <summary>
    /// 数据源
    /// </summary>
    [SugarColumn(ColumnDescription = "数据源", Length = 64)]
    [MaxLength(64)]
    public string? DataSource { get; set; }

    /// <summary>
    /// 分组Id
    /// </summary>
    [SugarColumn(ColumnDescription = "分组Id")]
    public long? GroupId { get; set; }

    /// <summary>
    /// 分组
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(GroupId))]
    public SysReportGroup Group { get; set; }

    /// <summary>
    /// 脚本语句
    /// </summary>
    [SugarColumn(ColumnDescription = "脚本语句", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? SqlScript { get; set; }

    /// <summary>
    /// 接口地址
    /// </summary>
    [SugarColumn(ColumnDescription = "接口地址", Length = 1024)]
    [MaxLength(1024)]
    public string? ApiUrl { get; set; }

    /// <summary>
    /// 接口请求方式
    /// </summary>
    [SugarColumn(ColumnDescription = "接口请求方式", Length = 16)]
    [MaxLength(16)]
    public string? ApiHttpMethod { get; set; }

    /// <summary>
    /// 接口参数
    /// </summary>
    [SugarColumn(ColumnDescription = "接口参数", Length = 1024)]
    [MaxLength(1024)]
    public string? ApiParams { get; set; }

    /// <summary>
    /// 参数
    /// </summary>
    [SugarColumn(ColumnDescription = "参数", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? Params { get; set; }

    /// <summary>
    /// 列表字段
    /// </summary>
    [SugarColumn(ColumnDescription = "列表字段", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? Fields { get; set; }
}