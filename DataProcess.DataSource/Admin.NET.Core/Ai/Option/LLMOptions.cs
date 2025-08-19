// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// LLM配置选项
/// 基于于Microsoft Semantic Kernel实现,也是本应用的默认实现
/// </summary>
public class LLMOptions : IConfigurableOptions
{
    public string ModelProvider { get; set; }
    public string InitSystemChatMessage { get; set; }
    public bool UserCanSwitchLLM { get; set; }
    public int TargetCount { get; set; }
    public int ThresholdCount { get; set; }
    public bool IsUserProxy { get; set; }
    public string ProxyUrl { get; set; }
    public bool LogEnabled { get; set; }
    public List<ProviderOptions> Providers { get; set; }
}

/// <summary>
/// LLM提供者选项
/// </summary>
public class ProviderOptions
{
    public string ProductName { get; set; }
    public string LLMType { get; set; }
    public string ApiKey { get; set; }
    public string ApiEndpoint { get; set; }
    public string ApiSecret { get; set; }
    public string Region { get; set; }
    public ChatCompletionOptions ChatCompletion { get; set; }
    public EmbeddingOptions Embedding { get; set; }
}

/// <summary>
/// 聊天完成选项
/// </summary>
public class ChatCompletionOptions
{
    public string ModelId { get; set; }
    public List<string> SupportModelIds { get; set; }
}

/// <summary>
/// 嵌入选项
/// </summary>
public class EmbeddingOptions
{
    public string ModelId { get; set; }
    public List<string> SupportModelIds { get; set; }
}