// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.Threading.Channels;

namespace Admin.NET.Core.Ai.Service;

public class ChatChannelManager : ISingleton
{
    private readonly Channel<DataActionInput> _channel;
    private readonly ILogger<ChatChannelManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ChatChannelManager(
        ILogger<ChatChannelManager> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _channel = Channel.CreateBounded<DataActionInput>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _ = Task.Run(async () =>
        {
            while (await _channel.Reader.WaitToReadAsync())
            {
                var action = await _channel.Reader.ReadAsync();
                try
                {
                    // 在需要时创建新的作用域
                    using var scope = _serviceProvider.CreateScope();
                    var chatChannelActionService = scope.ServiceProvider.GetRequiredService<ChatChannelActionService>();

                    switch (action.ActionType)
                    {
                        case ChatActionEnum.Append:
                            await chatChannelActionService.AppendAsync(action.Item);
                            break;

                        case ChatActionEnum.AppendItem:
                            await chatChannelActionService.AppendItemAsync(action.Item);
                            break;

                        case ChatActionEnum.DeleteAll:
                            await chatChannelActionService.DeleteAllAsync(action.Item);
                            break;

                        case ChatActionEnum.RenameSummary:
                            await chatChannelActionService.RenameSummaryAsync(action.Item);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理数据库操作时发生错误");
                }
            }
        });
    }

    public ChannelReader<DataActionInput> ActionReader => _channel.Reader;
    public ChannelWriter<DataActionInput> ActionWriter => _channel.Writer;
}