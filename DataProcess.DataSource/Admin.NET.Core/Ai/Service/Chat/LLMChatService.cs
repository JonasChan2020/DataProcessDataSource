// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Ai.Service;

/// <summary>
/// LLM聊天补全服务
/// </summary>
[ApiDescriptionSettings(Description = "LLM聊天补全服务", Name = "LLMChat", Order = 100)]
public class LLMChatService : IDynamicApiController, ITransient
{
    private readonly ILogger<LLMChatService> _logger;
    private readonly UserManager _userManager;
    private readonly SqlSugarRepository<SysUser> _userService;
    private readonly SqlSugarRepository<LLMChatHistory> _chatHistoryService;
    private readonly LLMOptionService _llmOptionService;
    private readonly LLMChatCoreService _chatCoreService; //聊天核心服务
    private readonly SqlSugarRepository<LLMChatSummaryHistory> _chatSummaryHistoryRep;

    public LLMChatService(ILogger<LLMChatService> logger,
        UserManager userManager,
        SqlSugarRepository<SysUser> userService,
        SqlSugarRepository<LLMChatHistory> chatHistoryService,
        LLMOptionService llmOptionService,
        SqlSugarRepository<LLMChatSummaryHistory> chatSummaryHistoryService,
        LLMChatCoreService chatCoreService)
    {
        _logger = logger;
        _userManager = userManager;
        _userService = userService;
        _chatHistoryService = chatHistoryService;
        _llmOptionService = llmOptionService;
        _chatCoreService = chatCoreService;
        _chatSummaryHistoryRep = chatSummaryHistoryService;
    }

    /// <summary>
    /// 聊天补全
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Description = "聊天补全", Name = "Chat")]
    public async Task<ChatOutput> ChatAsync(ChatInput message, CancellationToken cancellationToken) => await _chatCoreService.ChatAsync(message, cancellationToken);

    /// <summary>
    /// 删除所属摘要的所有聊天记录
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Description = "删除所属摘要的所有聊天记录", Name = "DeleteSummaryAll")]
    public async Task<bool> DeleteSummaryAllAsync(ChatInput message) => await _chatCoreService.DeleteSummaryAllAsync(message);

    /// <summary>
    /// 重命名所属摘要的标签
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Description = "重命名所属摘要的标签", Name = "RenameSummaryLable")]
    public async Task<bool> RenameSummaryLable(ChatInput message) => await _chatCoreService.RenameSummaryLable(message);

    /// <summary>
    /// 获取聊天列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Description = "获取聊天列表", Name = "ChatList"), HttpPost]
    public async Task<SqlSugarPagedList<ChatListOutput>> Page(ChatListInput input)
    {
        var userId = _userManager.UserId;
        if (userId == default)
        {
            throw new Exception("用户不存在");
        }
        var list = await _chatSummaryHistoryRep.AsQueryable().Includes(u => u.Histories)
            .Where(u => u.UserId == userId)
            .OrderBy(u => u.UtcCreateTime, OrderByType.Desc)
            .ToPagedListAsync(input.Page, input.PageSize);
        var returnList = list.Adapt<SqlSugarPagedList<ChatListOutput>>();
        return returnList;
    }

    /// <summary>
    /// 获取模型列表
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpGet]
    [ApiDescriptionSettings(Description = "获取模型列表", Name = "ModelList")]
    public async Task<ModelListOutput> GetModelListAsync() => await _llmOptionService.GetModelListAsync();

    /// <summary>
    /// 切换模型
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [ApiDescriptionSettings(Description = "切换模型", Name = "ChangeModel"), HttpPost]
    public async Task<bool> ChangeModelAsync(ModelListChangeInput input) => await _llmOptionService.ChangeModelAsync(input);
}