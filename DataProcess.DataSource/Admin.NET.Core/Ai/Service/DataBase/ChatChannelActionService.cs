// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Ai.Service;

/// <summary>
/// 异步对数据库进行操作
/// </summary>
public class ChatChannelActionService : IScoped
{
    private readonly ILogger<ChatChannelActionService> _logger;
    private readonly SqlSugarRepository<LLMChatSummaryHistory> _chatSummaryHistoryService;
    private readonly SqlSugarRepository<LLMChatHistory> _chatHistoryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="chatSummaryHistoryService"></param>
    /// <param name="chatHistoryService"></param>
    public ChatChannelActionService(ILogger<ChatChannelActionService> logger,
        SqlSugarRepository<LLMChatSummaryHistory> chatSummaryHistoryService,
        SqlSugarRepository<LLMChatHistory> chatHistoryService)
    {
        _logger = logger;
        _chatSummaryHistoryService = chatSummaryHistoryService;
        _chatHistoryService = chatHistoryService;
    }

    /// <summary>
    /// 追加聊天记录
    /// </summary>
    public async Task AppendAsync(LLMChatSummaryHistory chatSummaryHistory)
    {
        await _chatSummaryHistoryService.Context.CopyNew()
            .InsertNav(chatSummaryHistory)
            .Include(z => z.Histories)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 追加聊天明细
    /// </summary>
    public async Task AppendItemAsync(LLMChatSummaryHistory chatSummaryHistory)
    {
        chatSummaryHistory.Histories.ForEach(item => item.SummaryId = chatSummaryHistory.Id);
        await _chatHistoryService.Context.CopyNew()
            .Insertable(chatSummaryHistory.Histories)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除所有聊天记录
    /// </summary>
    public async Task DeleteAllAsync(LLMChatSummaryHistory chatSummaryHistory)
    {
        await _chatSummaryHistoryService.Context.CopyNew()
            .DeleteNav<LLMChatSummaryHistory>(u => u.Id == chatSummaryHistory.Id)
            .Include(u => u.Histories)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 重命名摘要
    /// </summary>
    public async Task RenameSummaryAsync(LLMChatSummaryHistory chatSummaryHistory)
    {
        var oldChatSummaryHistory = await _chatSummaryHistoryService.CopyNew()
            .AsQueryable()
            .Where(u => u.Id == chatSummaryHistory.Id)
            .FirstAsync();
        oldChatSummaryHistory.Summary = chatSummaryHistory.Summary;
        await _chatSummaryHistoryService.Context.CopyNew()
            .Updateable(oldChatSummaryHistory)
            .ExecuteCommandAsync();
    }
}