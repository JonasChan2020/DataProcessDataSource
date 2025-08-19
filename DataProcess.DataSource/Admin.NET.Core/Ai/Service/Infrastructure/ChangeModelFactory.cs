// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Amazon.BedrockRuntime;
using Amazon.Runtime;
using OllamaSharp;
using System.Net;

namespace Admin.NET.Core.Ai.Service;

/// <summary>
/// 模型切换工厂
/// 适用场景：需要切换模型时使用
/// </summary>
public class ChangeModelFactory : ILLMFactory, ITransient
{
    private readonly IServiceProvider _serviceProvider;

    public ChangeModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 创建Kernel实例
    /// </summary>
    /// <param name="modelInput">模型输入</param>
    /// <returns></returns>
    public Kernel CreateKernel(LLMModelInput modelInput)
    {
        var builder = Kernel.CreateBuilder();
        return GetKernel(builder, modelInput);
    }

    /// <summary>
    /// 获取Kernel实例
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="modelInput"></param>
    /// <returns></returns>
    private Kernel GetKernel(IKernelBuilder builder, LLMModelInput modelInput)
    {
        var llmOptions = App.GetOptions<LLMOptions>();
        HttpClient httpClient = GetHttpClient(llmOptions);
        ConfigureKernel(builder, httpClient, modelInput);
        var kernel = builder.Build();
        return kernel;
    }

    /// <summary>
    /// 配置Kernel
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="httpClient"></param>
    /// <param name="modelInput"></param>
    private static void ConfigureKernel(IKernelBuilder builder, HttpClient httpClient, LLMModelInput modelInput)
    {
        var llmOptions = App.GetOptions<LLMOptions>();
        var provider = llmOptions.Providers.FirstOrDefault(p => p.ProductName == modelInput.ProductName && p.ChatCompletion.SupportModelIds.Contains(modelInput.ModelId));
        if (provider == null)
        {
            throw new Exception($"未找到{modelInput.ProductName}:{modelInput.ModelId}模型提供者");
        }
        ConfigureKernelCore(builder, httpClient, provider, modelInput.ModelId);
    }

    /// <summary>
    /// 配置Kernel核心代码
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="httpClient"></param>
    /// <param name="provider"></param>
    /// <param name="mid"></param>
    private static void ConfigureKernelCore(IKernelBuilder builder, HttpClient httpClient, ProviderOptions provider, string mid)
    {
        var apiKey = provider.ApiKey;
        var apiEndpoint = new Uri(provider.ApiEndpoint);
        var modelId = mid;
        switch (provider.LLMType)
        {
            case "OpenAI_Compatible":
                builder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    endpoint: apiEndpoint,
                    apiKey: apiKey,
                    httpClient: httpClient
                );
                if (provider.ProductName == "OpenAI")
                {
#pragma warning disable SKEXP0010 // 禁用预览API的警告
                    // 如果模型是OpenAI，则添加音频转文字服务
                    builder.AddOpenAIAudioToText(
                        modelId: modelId,
                        apiKey: apiKey,
                        httpClient: httpClient
                    );
                    // 如果模型是OpenAI，则添加文字转音频服务
                    builder.AddOpenAITextToAudio(
                        modelId: modelId,
                        apiKey: apiKey,
                        httpClient: httpClient
                    );
                    // 如果模型是OpenAI，则添加文字转图片服务
                    builder.AddOpenAITextToImage(
                        modelId: modelId,
                        apiKey: apiKey,
                        httpClient: httpClient
                    );
#pragma warning restore
                }
                break;

            case "AzureOpenAI":
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: modelId,
                    endpoint: provider.ApiEndpoint,
                    apiKey: apiKey,
                    httpClient: httpClient
                );
#pragma warning disable SKEXP0010 // 禁用预览API的警告
                builder.AddAzureOpenAIAudioToText(
                    deploymentName: modelId,
                    endpoint: provider.ApiEndpoint,
                    apiKey: apiKey,
                    httpClient: httpClient
                );
                builder.AddAzureOpenAITextToAudio(
                    deploymentName: modelId,
                    endpoint: provider.ApiEndpoint,
                    apiKey: apiKey,
                    httpClient: httpClient
                );
                builder.AddAzureOpenAITextToImage(
                    deploymentName: modelId,
                    endpoint: provider.ApiEndpoint,
                    apiKey: apiKey,
                    httpClient: httpClient
                );
#pragma warning restore SKEXP0010 // 禁用预览API的警告
                break;

            case "Ollama":
                var ollamaClient = new OllamaApiClient(
                    uriString: provider.ApiEndpoint,
                    defaultModel: modelId
                );
#pragma warning disable SKEXP0070// 禁用预览API的警告
                builder.AddOllamaChatCompletion(
                    ollamaClient: ollamaClient
                );
#pragma warning restore SKEXP0070// 禁用预览API的警告
                break;

            case "Claude":
                var credentials = new BasicAWSCredentials(provider.ApiKey, provider.ApiSecret);
                var amazonBedrockRuntime = new AmazonBedrockRuntimeClient(credentials, new AmazonBedrockRuntimeConfig
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(provider.Region)
                });
#pragma warning disable SKEXP0070
                builder.AddBedrockChatCompletionService(
                    modelId: modelId,
                    bedrockRuntime: amazonBedrockRuntime
                );
#pragma warning restore SKEXP0070
                break;

            default:
                throw new Exception($"未找到{provider.LLMType}模型提供者");
        }
    }

    /// <summary>
    /// 获取HttpClient实例,可能为null
    /// </summary>
    /// <param name="llmOptions"></param>
    /// <returns></returns>
    private static HttpClient GetHttpClient(LLMOptions llmOptions)
    {
        if (!llmOptions.LogEnabled && !llmOptions.IsUserProxy)
            return null;
        HttpMessageHandler httpMessageHandler = null;
        //设置日志
        if (llmOptions.LogEnabled)
        {
            if (llmOptions.IsUserProxy)
            {
                httpMessageHandler = new LoggingHandler(new HttpClientHandler()
                {
                    Proxy = new WebProxy(llmOptions.ProxyUrl),
                    UseProxy = true,
                });
            }
            else
            {
                httpMessageHandler = new LoggingHandler(new HttpClientHandler());
            }
        }
        else
        {
            if (llmOptions.IsUserProxy)
            {
                httpMessageHandler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(llmOptions.ProxyUrl),
                    UseProxy = true,
                };
            }
        }
        return httpMessageHandler == null ? null : new HttpClient(httpMessageHandler);
    }
}