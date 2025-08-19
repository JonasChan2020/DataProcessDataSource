// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Ai.Service;

/// <summary>
/// SSE服务
/// </summary>
[ApiController]
public class SseService : ControllerBase
{
    private readonly ILogger<SseService> _logger;
    private readonly SseChannelManager _sseChannelManager;
    private readonly SseDeepThinkingChannelManager _sseDeepThinkingChannelManager;

    public SseService(ILogger<SseService> logger, SseChannelManager sseChannelManager, SseDeepThinkingChannelManager sseDeepThinkingChannelManager)
    {
        _logger = logger;
        _sseChannelManager = sseChannelManager;
        _sseDeepThinkingChannelManager = sseDeepThinkingChannelManager;
        _logger.LogInformation("SseService 初始化!测试guid串：{Guid}", Guid.NewGuid().ToString());
    }

    [HttpGet("/sse/chat/{id}")]
    [AllowAnonymous]
    public async Task Chat(long id, CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Nginx

        var channel = _sseChannelManager.Register(id);
        var deepThinkingChannel = _sseDeepThinkingChannelManager.Register(id);
        try
        {
            _ = Task.Run(async () =>
            {
                //心跳检查
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Response.WriteAsync($"event: ping\n");
                    await Response.WriteAsync($"data: pong\n\n");
                    await Response.Body.FlushAsync(cancellationToken);
                    await Task.Delay(3000, cancellationToken);
                }
            });
            _ = Task.Run(async () =>
            {
                await foreach (var message in deepThinkingChannel.ReadAllAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Response.WriteAsync($"event: deepThinking\n");
                    await Response.WriteAsync($"data: {message}\n\n");
                    await Response.Body.FlushAsync(cancellationToken);
                }
            });
            await foreach (var message in channel.ReadAllAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Response.WriteAsync($"event: chat\n");
                await Response.WriteAsync($"data: {message}\n\n");
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SseService 取消!");
        }
        finally
        {
            _sseChannelManager.Unregister(id);
            _sseDeepThinkingChannelManager.Unregister(id);
        }
    }
}