// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 导入/导出职位数据映射
/// </summary>
[ExcelImporter(IsLabelingError = true)]
[ExcelExporter(Name = "职位数据", TableStyle = OfficeOpenXml.Table.TableStyles.None, AutoFitAllColumn = true)]
public class PosDto
{
    /// <summary>
    /// 名称
    /// </summary>
    [ImporterHeader(Name = "名称")]
    [ExporterHeader(DisplayName = "名称", IsBold = true)]
    public string Name { get; set; }

    /// <summary>
    /// 编码
    /// </summary>
    [ImporterHeader(Name = "编码")]
    [ExporterHeader(DisplayName = "编码", IsBold = true)]
    public string Code { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [ImporterHeader(Name = "排序")]
    [ExporterHeader(DisplayName = "排序", IsBold = true)]
    public int OrderNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [ImporterHeader(Name = "备注")]
    [ExporterHeader(DisplayName = "备注", IsBold = true)]
    public string Remark { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [ImporterHeader(Name = "状态")]
    [Required(ErrorMessage = "状态不能为空")]
    [ValueMapping("启用", 1)]
    [ValueMapping("停用", 2)]
    [ExporterHeader(DisplayName = "状态", IsBold = true)]
    public StatusEnum Status { get; set; }
}