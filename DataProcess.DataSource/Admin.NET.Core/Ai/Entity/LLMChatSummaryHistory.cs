// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 聊天摘要历史表
/// </summary>
[SugarTable(null, "AI聊天摘要历史表")]
[SugarIndex("i_{table}_us", nameof(UserId), OrderByType.Asc)]
[SugarIndex("i_{table}_un", nameof(UniqueToken), OrderByType.Asc)]
public class LLMChatSummaryHistory : EntityBaseId
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID")]
    public long UserId { get; set; }

    /// <summary>
    /// 唯一标识
    /// </summary>
    [SugarColumn(ColumnDescription = "唯一标识")]
    public string UniqueToken { get; set; }

    /// <summary>
    /// 摘要
    /// </summary>
    [SugarColumn(ColumnDescription = "摘要", Length = 1024)]
    [MaxLength(1024)]
    public string? Summary { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnDescription = "创建时间")]
    public long UtcCreateTime { get; set; } = DateTime.UtcNow.ToLong();

    /// <summary>
    /// 聊天历史
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(LLMChatHistory.SummaryId))]
    public List<LLMChatHistory>? Histories { get; set; }
}