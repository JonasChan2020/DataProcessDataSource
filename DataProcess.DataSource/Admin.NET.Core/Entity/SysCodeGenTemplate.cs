// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 代码生成模板配置表
/// </summary>
[SugarTable(null, "代码生成模板配置表")]
[SysTable]
public partial class SysCodeGenTemplate : EntityBase
{
    /// <summary>
    /// 模板文件名
    /// </summary>
    [SugarColumn(ColumnDescription = "模板文件名", Length = 128)]
    [Required, MaxLength(128)]
    public string Name { get; set; }

    /// <summary>
    /// 代码生成类型
    /// </summary>
    [SugarColumn(ColumnDescription = "代码生成类型", DefaultValue = "2")]
    public CodeGenTypeEnum Type { get; set; } = CodeGenTypeEnum.Backend;

    /// <summary>
    /// 是否是内置模板（Y-是，N-否）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否是内置模板", DefaultValue = "1")]
    public YesNoEnum SysFlag { get; set; } = YesNoEnum.Y;

    /// <summary>
    /// 是否默认
    /// </summary>
    [SugarColumn(ColumnDescription = "是否默认")]
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 输出位置
    /// </summary>
    [SugarColumn(ColumnDescription = "输出位置", Length = 256)]
    [Required, MaxLength(256)]
    public string OutputFile { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnDescription = "描述", Length = 256)]
    [Required, MaxLength(256)]
    public string Describe { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序", DefaultValue = "100")]
    public int OrderNo { get; set; } = 100;
}