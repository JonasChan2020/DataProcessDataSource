// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Newtonsoft.Json;

namespace Admin.NET.Core.Ai.Service;

/// <summary>
/// OpenRouter 客户端
/// </summary>
public class LLMOpenRouterClient
{
    private readonly HttpClient _httpClient;
    private readonly LLMCustomOptions _options;

    public LLMOpenRouterClient(HttpClient httpClient, IOptions<LLMCustomOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.Timeout);
    }

    /// <summary>
    /// 获取提示词的LLM的回答
    /// </summary>
    /// <param name="messages">提示词</param>
    /// <returns>提示词的LLM的回答</returns>
    /// <exception cref="ArgumentException">消息列表不能为空</exception>
    /// <exception cref="Exception">网络请求错误</exception>
    /// <exception cref="JsonException">JSON解析错误</exception>
    public async Task<string> GetPromptAsync(List<LLMInputMessage> messages)
    {
        return await GetLLMResponseAsync(messages, (messages) =>
        {
            if (!messages.Any(m => m.Role.Equals("system")))
            {
                messages.Insert(0, new LLMInputMessage()
                {
                    Role = "system",
                    Content = _options.InitSystemPromptMessage
                });
            }
        });
    }

    /// <summary>
    /// 获取聊天记录的LLM的回答
    /// </summary>
    /// <param name="messages">聊天记录</param>
    /// <returns>聊天记录的LLM的回答</returns>
    /// <exception cref="ArgumentException">消息列表不能为空</exception>
    /// <exception cref="Exception">网络请求错误</exception>
    public async Task<string> GetChatAsync(List<LLMInputMessage> messages)
    {
        return await GetLLMResponseAsync(messages, (messages) =>
        {
            if (!messages.Any(m => m.Role.Equals("system")))
            {
                messages.Insert(0, new LLMInputMessage()
                {
                    Role = "system",
                    Content = _options.InitSystemChatMessage
                });
            }
        });
    }

    /// <summary>
    /// 获取LLM的回答
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="beforeSendAction">在发送请求之前，可以对消息进行修改</param>
    /// <returns>LLM的回答</returns>
    /// <exception cref="ArgumentException">消息列表不能为空</exception>
    /// <exception cref="Exception">网络请求错误</exception>
    /// <exception cref="JsonException">JSON解析错误</exception>
    private async Task<string> GetLLMResponseAsync(List<LLMInputMessage> messages, Action<List<LLMInputMessage>> beforeSendAction = null)
    {
        try
        {
            if (messages == null || !messages.Any())
                throw new ArgumentException("Message list cannot be empty");

            if (messages.Any(m => m.Content.Length > 2000))
                throw new ArgumentException("Message content exceeds the maximum length limit");

            var defaultLLM = _options.SupportLLMList.Find(item => item.Desciption.Equals(_options.ModelProvider));
            if (defaultLLM == null)
            {
                throw new Exception("Default LLM not found, please check if the ModelProvider in ai.json is set incorrectly?");
            }
            var inputBody = new LLMInputBody();
            inputBody.Model = defaultLLM.Model;
            inputBody.Messages = messages;
            var strBody = LLMJsonTool.SerializeObject(inputBody);
            beforeSendAction?.Invoke(messages);  // 在发送请求之前，可以对消息进行修改
            using (var content = new StringContent(strBody, Encoding.UTF8, "application/json"))
            using (var response = await _httpClient.PostAsync(_options.BaseUrl, content))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var strResponse = await response.Content.ReadAsStringAsync();
                    var output = LLMJsonTool.DeserializeObject<LLMOutput>(strResponse);
                    return output.Choices[0].Message.Content;
                }
                else
                {
                    throw new Exception("Failed to get LLM response: " + $"Status code: {response.StatusCode}" + " " + $"Error message: {response.ReasonPhrase}" + " " + $"Error content: {await response.Content.ReadAsStringAsync()}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network request error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            throw new Exception($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Unknown error: {ex.Message}");
        }
    }
}