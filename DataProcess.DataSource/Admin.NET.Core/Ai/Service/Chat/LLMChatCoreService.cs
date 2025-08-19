// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Microsoft.SemanticKernel.ChatCompletion;

namespace Admin.NET.Core.Ai.Service;

/// <summary>
/// 聊天补全核心服务
/// </summary>
public class LLMChatCoreService : ITransient
{
    private readonly ILogger<LLMChatCoreService> _logger;
    private readonly IChatHistoryReducer _chatHistoryTruncationReducer;  //截取器

    // private readonly IChatHistoryReducer _chatHistorySummarizationReducer;  //摘要器
    private readonly ILLMFactory _llmFactory;

    private readonly SseChannelManager _sseChannelManager;   //给sse服务发送消息
    private readonly SseDeepThinkingChannelManager _sseDeepThinkingChannelManager;   //给sse服务发送深度思考消息
    private readonly ChatChannelManager _dbAction; //操作数据库
    private readonly SqlSugarRepository<LLMChatSummaryHistory> _chatSummaryHistoryService;
    private readonly IOptions<LLMOptions> _llmOption;
    private readonly UserManager _userManager;
    private readonly SysCacheService _sysCacheService;

    public LLMChatCoreService(ILogger<LLMChatCoreService> logger,
        ILLMFactory llmFactory,
        SseChannelManager sseChannelManager,
        SseDeepThinkingChannelManager sseDeepThinkingChannelManager,
        ChatChannelManager chatChannelManager,
        IOptions<LLMOptions> llmOption,
        SysCacheService sysCacheService,
        UserManager userManager,
        SqlSugarRepository<LLMChatSummaryHistory> chatSummaryHistoryService)
    {
        _sysCacheService = sysCacheService;
        _logger = logger;
        _llmFactory = llmFactory;
        _sseChannelManager = sseChannelManager;
        _sseDeepThinkingChannelManager = sseDeepThinkingChannelManager;
        _dbAction = chatChannelManager;
        _llmOption = llmOption;
        _userManager = userManager;
        _chatSummaryHistoryService = chatSummaryHistoryService;
        _chatHistoryTruncationReducer = new ChatHistoryTruncationReducer(_llmOption.Value.TargetCount, _llmOption.Value.ThresholdCount);
    }

    /// <summary>
    /// 聊天补全
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ChatOutput> ChatAsync(ChatInput input, CancellationToken cancellationToken)
    {
        var kernel = _llmFactory.CreateKernel(new LLMModelInput
        {
            ProductName = input.ProviderName,
            ModelId = input.ModelId
        });
        if (input.UniqueToken == "add_new_chat")
        {
            return await NewChatAsync(input, cancellationToken, kernel);
        }
        else
        {
            return await ContinueChatAsync(input, cancellationToken, kernel);
        }
    }

    #region 新聊天

    /// <summary>
    /// 新聊天
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="kernel"></param>
    /// <returns></returns>
    private async Task<ChatOutput> NewChatAsync(ChatInput input, CancellationToken cancellationToken, Kernel kernel)
    {
        if (input.DeepThinking)
        {
            return await DeepThinkingNewChatCore(input, kernel, cancellationToken);
        }
        else
        {
            return await NormalNewChatCore(input, kernel, cancellationToken);
        }
    }

    /// <summary>
    /// 深度思考新聊天
    /// </summary>
    /// <param name="input"></param>
    /// <param name="kernel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<ChatOutput> DeepThinkingNewChatCore(ChatInput input, Kernel kernel, CancellationToken cancellationToken)
    {
        ChatHistory chatHistory = [];

        #region 深度思考过程

        var deepThinkingPrompt = $"""
        你是一个逻辑严谨的AI助手，请在回答前，先梳理出完整的推理路径,用户的问题是：
        ```
        {input.Message}
        ```
        请你一步步进行推理，不要给出最终答案，仅展示思考过程，适当加入emoji表达人类情感，**用第一人称描述**。
        """;
        chatHistory.AddSystemMessage(deepThinkingPrompt);
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var thinkingMessage = "";
        var response = chat.GetStreamingChatMessageContentsAsync(chatHistory: chatHistory, kernel: kernel, cancellationToken: cancellationToken);
        var thinkBegin = true;
        //发送消息到SSE通道
        await foreach (var chunk in response)
        {
            if (thinkBegin)
            {
                thinkBegin = false;
                await _sseDeepThinkingChannelManager.SendMessageAsync(_userManager.UserId, "[BEGIN]", cancellationToken);
            }
            thinkingMessage += chunk.Content ?? "";
            var streamInput = chunk.Content ?? "";
            streamInput = streamInput.Replace("\n", "\\x0A");
            //发送消息到SSE通道
            await _sseDeepThinkingChannelManager.SendMessageAsync(_userManager.UserId, streamInput, cancellationToken);
        }
        await _sseDeepThinkingChannelManager.SendMessageAsync(_userManager.UserId, "[DONE]", cancellationToken);

        #endregion 深度思考过程

        //生成最终答案
        return await NormalNewChatCore(input, kernel, cancellationToken, thinkingMessage);
    }

    /// <summary>
    /// 普通新聊天
    /// </summary>
    /// <param name="input"></param>
    /// <param name="kernel"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="thinkingMessage"></param>
    /// <returns></returns>
    private async Task<ChatOutput> NormalNewChatCore(ChatInput input, Kernel kernel, CancellationToken cancellationToken, string thinkingMessage = "")
    {
        ChatHistory chatHistory = [];
        chatHistory.AddSystemMessage(_llmOption.Value.InitSystemChatMessage == null || _llmOption.Value.InitSystemChatMessage == "" ?
            "你是一个AI助手，请根据用户的问题给出回答。" : _llmOption.Value.InitSystemChatMessage);
        if (thinkingMessage != "")
        {
            var chatPrompt = $"""
            问题：{input.Message}
            思考过程：
            {thinkingMessage}
            请基于以上思考，得出结论，并输出最终答案。
            """;
            chatHistory.AddUserMessage(chatPrompt);
        }
        else
        {
            chatHistory.AddUserMessage(input.Message);
        }
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var message = "";
        var response = chat.GetStreamingChatMessageContentsAsync(chatHistory: chatHistory, kernel: kernel, cancellationToken: cancellationToken);
        var beginStream = true;
        //发送消息到SSE通道
        await foreach (var chunk in response)
        {
            if (beginStream)
            {
                beginStream = false;
                await _sseChannelManager.SendMessageAsync(_userManager.UserId, "[BEGIN]", cancellationToken);
            }
            message += chunk.Content ?? "";
            var streamInput = chunk.Content ?? "";
            streamInput = streamInput.Replace("\n", "\\x0A");
            //发送消息到SSE通道
            await _sseChannelManager.SendMessageAsync(_userManager.UserId, streamInput, cancellationToken);
        }
        await _sseChannelManager.SendMessageAsync(_userManager.UserId, "[DONE]", cancellationToken);
        //保存聊天记录
        return await SaveAddData(input, message, cancellationToken);
    }

    private async Task<ChatOutput> SaveAddData(ChatInput input, string message, CancellationToken cancellationToken)
    {
        ChatHistory chatHistory = [];
        chatHistory.AddSystemMessage(_llmOption.Value.InitSystemChatMessage == null || _llmOption.Value.InitSystemChatMessage == "" ?
            "你是一个AI助手，请根据用户的问题给出回答。" : _llmOption.Value.InitSystemChatMessage);
        chatHistory.AddUserMessage(input.Message);
        chatHistory.AddAssistantMessage(message);
        return await _AddToDatabase(chatHistory, cancellationToken);
    }

    private async Task<ChatOutput> _AddToDatabase(ChatHistory chatHistory, CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N");
        LLMChatSummaryHistory chatSummaryHistory = new LLMChatSummaryHistory
        {
            UserId = _userManager.UserId,
            UniqueToken = token,
            Summary = "New Chat",
            UtcCreateTime = DateTime.UtcNow.ToLong(),
            Histories = chatHistory.Select(x => new LLMChatHistory
            {
                UserId = _userManager.UserId,
                Content = x.Content,
                UtcCreateTime = DateTime.UtcNow.ToLong(),
                Role = x.Role.ToString(),
            }).ToList(),
        };
        DataActionInput dataActionInput = new DataActionInput
        {
            ActionType = ChatActionEnum.Append,
            Item = chatSummaryHistory
        };
        await _dbAction.ActionWriter.WriteAsync(dataActionInput);
        return new ChatOutput
        {
            UniqueToken = token,
            Note = "New Chat",
            State = true,
            Summary = "New Chat"
        };
    }

    #endregion 新聊天

    #region 续聊

    /// <summary>
    /// 续聊
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="kernel"></param>
    /// <returns></returns>
    private async Task<ChatOutput> ContinueChatAsync(ChatInput input, CancellationToken cancellationToken, Kernel kernel)
    {
        var uniqueToken = input.UniqueToken;
        var chatHistory = await _GetChatHistory(uniqueToken, input.Message, input.DeepThinking);
        if (input.DeepThinking)
        {
            return await DeepThinkingContinueChatCoreAsync(input, kernel, uniqueToken, chatHistory, cancellationToken);
        }
        else
        {
            return await NormalContinueChatCoreAsync(input, kernel, uniqueToken, chatHistory, cancellationToken);
        }
    }

    /// <summary>
    /// 深度思考续聊
    /// </summary>
    /// <param name="input"></param>
    /// <param name="kernel"></param>
    /// <param name="uniqueToken"></param>
    /// <param name="chatHistory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<ChatOutput> DeepThinkingContinueChatCoreAsync(ChatInput input, Kernel kernel, string uniqueToken, ChatHistory chatHistory, CancellationToken cancellationToken)
    {
        #region 深度思考过程

        var deepThinkChatHistory = new ChatHistory(chatHistory);
        var deepThinkingPrompt = $"""
        你是一个逻辑严谨的AI助手，请在回答前，先梳理出完整的推理路径,请你一步步进行推理，不要给出最终答案，仅展示思考过程，适当加入emoji表达人类情感，**用第一人称描述**。
        """;
        deepThinkChatHistory.AddSystemMessage(deepThinkingPrompt);
        deepThinkChatHistory.AddUserMessage(input.Message);
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var thinkingMessage = "";
        var thinkBegin = true;
        var response = chat.GetStreamingChatMessageContentsAsync(chatHistory: deepThinkChatHistory, kernel: kernel, cancellationToken: cancellationToken);
        await foreach (var chunk in response)
        {
            if (thinkBegin)
            {
                thinkBegin = false;
                await _sseDeepThinkingChannelManager.SendMessageAsync(_userManager.UserId, "[BEGIN]", cancellationToken);
            }
            thinkingMessage += chunk.Content ?? "";
            var streamInput = chunk.Content ?? "";
            streamInput = streamInput.Replace("\n", "\\x0A");
            //发送消息到SSE通道
            await _sseDeepThinkingChannelManager.SendMessageAsync(_userManager.UserId, streamInput, cancellationToken);
        }
        await _sseDeepThinkingChannelManager.SendMessageAsync(_userManager.UserId, "[DONE]", cancellationToken);

        #endregion 深度思考过程

        //生成最终答案
        return await NormalContinueChatCoreAsync(input, kernel, uniqueToken, chatHistory, cancellationToken, thinkingMessage);
    }

    /// <summary>
    /// 普通续聊
    /// </summary>
    /// <param name="input"></param>
    /// <param name="kernel"></param>
    /// <param name="uniqueToken"></param>
    /// <param name="chatHistory"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="thinkingMessage"></param>
    /// <returns></returns>
    private async Task<ChatOutput> NormalContinueChatCoreAsync(ChatInput input, Kernel kernel, string uniqueToken, ChatHistory chatHistory, CancellationToken cancellationToken, string thinkingMessage = "")
    {
        ChatHistory updateChatHistory = [];
        updateChatHistory.AddUserMessage(input.Message);
        if (thinkingMessage != "")
        {
            var deepThinkingPrompot = $"""
            问题：{input.Message}
            思考过程：
            {thinkingMessage}
            请基于以上思考，得出结论，并输出最终答案。
            """;
            chatHistory.AddUserMessage(deepThinkingPrompot);
        }
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var message = "";
        var beginStream = true;
        await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(chatHistory: chatHistory, kernel: kernel, cancellationToken: cancellationToken))
        {
            if (beginStream)
            {
                beginStream = false;
                await _sseChannelManager.SendMessageAsync(_userManager.UserId, "[BEGIN]", cancellationToken);
            }
            message += chunk.Content ?? "";
            var streamInput = chunk.Content ?? "";
            streamInput = streamInput.Replace("\n", "\\x0A");
            //发送消息到SSE通道
            await _sseChannelManager.SendMessageAsync(_userManager.UserId, streamInput, cancellationToken);
        }
        await _sseChannelManager.SendMessageAsync(_userManager.UserId, "[DONE]", cancellationToken);
        updateChatHistory.AddAssistantMessage(message);
        return await SaveUpdate(input, kernel, uniqueToken, chatHistory, updateChatHistory, message, cancellationToken);
    }

    private async Task<ChatOutput> SaveUpdate(ChatInput input, Kernel kernel, string uniqueToken, ChatHistory chatHistory, ChatHistory updateChatHistory, string message, CancellationToken cancellationToken)
    {
        chatHistory.AddAssistantMessage(message);
        await _SaveChatItemToDatabase(updateChatHistory, uniqueToken);
        var summary = await _ProcessSummary(chatHistory, cancellationToken, kernel);
        await _CheckSummaryId(input);
        await _UpdateSummaryToDatabase(summary, input);
        return new ChatOutput
        {
            UniqueToken = uniqueToken,
            Note = "continue chat",
            Summary = summary == "" ? input.Summary : summary,
            State = true,
            SummaryId = input.SummaryId
        };
    }

    /// <summary>
    /// 检查摘要ID,因为前端记录的是临时摘要ID,所以需要检查
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task _CheckSummaryId(ChatInput input)
    {
        var chatSummaryHistory = await _chatSummaryHistoryService.AsQueryable().Where(u => u.UniqueToken == input.UniqueToken).FirstAsync();
        if (chatSummaryHistory == null)
        {
            throw new Exception("聊天摘要记录不存在");
        }
        input.SummaryId = chatSummaryHistory.Id;
    }

    private async Task _UpdateSummaryToDatabase(string summary, ChatInput input)
    {
        //更新摘要至数据库
        if (summary != "" && input.SummaryId != 0)
        {
            await _dbAction.ActionWriter.WriteAsync(new DataActionInput
            {
                ActionType = ChatActionEnum.RenameSummary,
                Item = new LLMChatSummaryHistory
                {
                    Id = input.SummaryId,
                    Summary = summary == "" ? input.Summary : summary
                }
            });
        }
    }

    /// <summary>
    /// 处理摘要
    /// </summary>
    /// <param name="chatHistory"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="kernel"></param>
    /// <returns></returns>
    private async Task<string> _ProcessSummary(ChatHistory chatHistory, CancellationToken cancellationToken, Kernel kernel)
    {
        var filterChatHistory = chatHistory.Where(x => x.Role != AuthorRole.System).ToList<ChatMessageContent>();
        //如果聊天记录为第二轮或第六轮，则自动进行摘要.
        if (filterChatHistory.Count != 4 && filterChatHistory.Count != 12)
        {
            return "";
        }
        var summaryPrompt = "你是摘要专家，请根据历史对话内容，生成一个不超过13个字的简洁标题摘要，不要添加解释说明，只输出摘要内容本身,生成风格像 ChatGPT 聊天记录列表中显示的摘要。";
        filterChatHistory.Add(new ChatMessageContent(AuthorRole.User, summaryPrompt));
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory(filterChatHistory);
        var result = await chat.GetChatMessageContentAsync(chatHistory: history,
            kernel: kernel,
            cancellationToken: cancellationToken
        );
        return result.Content ?? "";
    }

    /// <summary>
    /// 获取聊天历史,并截取
    /// </summary>
    /// <param name="uniqueToken"></param>
    /// <param name="message"></param>
    /// <param name="deepThinking"></param>
    /// <returns></returns>
    private async Task<ChatHistory> _GetChatHistory(string uniqueToken, string message, bool deepThinking = false)
    {
        var chatSummaryHistory = await _chatSummaryHistoryService.AsQueryable().Includes(u => u.Histories).Where(u => u.UniqueToken == uniqueToken).FirstAsync();
        if (chatSummaryHistory == null)
        {
            throw new Exception("聊天记录不存在");
        }
        ChatHistory chatHistory = [];
        foreach (var history in chatSummaryHistory.Histories)
        {
            switch (history.Role)
            {
                case "user":
                    chatHistory.AddUserMessage(history.Content);
                    break;

                case "assistant":
                    chatHistory.AddAssistantMessage(history.Content);
                    break;

                case "system":
                    chatHistory.AddSystemMessage(history.Content);
                    break;

                default:
                    break;
            }
        }
        var reducedMessages = await _chatHistoryTruncationReducer.ReduceAsync(chatHistory);
        if (reducedMessages is not null)
        {
            chatHistory = new ChatHistory(reducedMessages);
        }
        if (!deepThinking)
        {
            chatHistory.AddUserMessage(message);
        }
        return chatHistory;
    }

    private async Task _SaveChatItemToDatabase(ChatHistory chatHistory, string uniqueToken)
    {
        var chatSummaryHistory = await _chatSummaryHistoryService.AsQueryable().Where(u => u.UniqueToken == uniqueToken).FirstAsync();
        if (chatSummaryHistory == null)
        {
            throw new Exception("聊天摘要记录不存在");
        }
        chatSummaryHistory.Histories = [];
        foreach (var history in chatHistory)
        {
            chatSummaryHistory.Histories.Add(new LLMChatHistory
            {
                UserId = _userManager.UserId,
                Content = history.Content,
                UtcCreateTime = DateTime.UtcNow.ToLong(),
                Role = history.Role.ToString(),
                SummaryId = chatSummaryHistory.Id
            });
        }
        DataActionInput dataActionInput = new DataActionInput
        {
            ActionType = ChatActionEnum.AppendItem,
            Item = chatSummaryHistory
        };
        await _dbAction.ActionWriter.WriteAsync(dataActionInput);
    }

    #endregion 续聊

    #region 删除所属摘要的所有聊天记录

    /// <summary>
    /// 删除所属摘要的所有聊天记录
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<bool> DeleteSummaryAllAsync(ChatInput input)
    {
        var chatSummaryHistory = await _chatSummaryHistoryService.AsQueryable().Where(u => u.Id == input.SummaryId).FirstAsync();
        if (chatSummaryHistory == null)
        {
            throw new Exception("聊天摘要记录不存在");
        }
        DataActionInput dataActionInput = new DataActionInput
        {
            ActionType = ChatActionEnum.DeleteAll,
            Item = chatSummaryHistory
        };
        await _dbAction.ActionWriter.WriteAsync(dataActionInput);
        return true;
    }

    #endregion 删除所属摘要的所有聊天记录

    #region 重命名所属摘要的标签

    /// <summary>
    /// 重命名所属摘要的标签
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<bool> RenameSummaryLable(ChatInput input)
    {
        var chatSummaryHistory = await _chatSummaryHistoryService.AsQueryable().Where(u => u.Id == input.SummaryId).FirstAsync();
        if (chatSummaryHistory == null)
        {
            throw new Exception("聊天摘要记录不存在");
        }
        chatSummaryHistory.Summary = input.Summary;
        DataActionInput dataActionInput = new DataActionInput
        {
            ActionType = ChatActionEnum.RenameSummary,
            Item = chatSummaryHistory
        };
        await _dbAction.ActionWriter.WriteAsync(dataActionInput);
        return true;
    }

    #endregion 重命名所属摘要的标签
}