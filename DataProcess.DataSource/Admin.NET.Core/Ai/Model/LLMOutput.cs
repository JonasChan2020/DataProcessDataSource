// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Newtonsoft.Json;

namespace Admin.NET.Core;

/// <summary>
/// LLM输出
/// </summary>
public class LLMOutput
{
    public string Id { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public List<ChoicesItem> Choices { get; set; }
    public Usage Usage { get; set; }
}

/// <summary>
/// LLM输出机会
/// </summary>
public class ChoicesItem
{
    [JsonProperty("logprobs")]
    public string? Logprobs { get; set; }

    public string? FinishReason { get; set; }
    public string? NativeFinishReason { get; set; }
    public int Index { get; set; }
    public OutPutMessage Message { get; set; }
}

/// <summary>
/// LLM输出消息
/// </summary>
public class OutPutMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public Object Refusal { get; set; }

    [JsonProperty("reasoning")]
    public string? Reasoning { get; set; }
}

/// <summary>
/// LLM的消耗
/// </summary>
public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}