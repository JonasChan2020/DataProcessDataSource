// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.Threading.Channels;

namespace Admin.NET.Core.Ai.Service;

/// <summary>
/// SSE通道管理
/// </summary>
public class BaseSseChannelManager : ISseChannelManager
{
    private readonly ConcurrentDictionary<long, Channel<string>> _userChannels = new();

    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public ChannelReader<string> Register(long userId)
    {
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        _userChannels[userId] = channel;
        return channel.Reader;
    }

    /// <summary>
    /// 注销
    /// </summary>
    /// <param name="userId"></param>
    public void Unregister(long userId)
    {
        if (_userChannels.TryRemove(userId, out var channel))
        {
            channel.Writer.TryComplete(); // 结束读端
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    public async Task SendMessageAsync(long userId, string message, CancellationToken cancellationToken = default)
    {
        if (_userChannels.TryGetValue(userId, out var channel))
        {
            await channel.Writer.WriteAsync(message, cancellationToken);
        }
    }
}