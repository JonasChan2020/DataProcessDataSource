// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using MQTTnet.Server;

namespace Admin.NET.Core;

/// <summary>
/// MQTT 事件拦截器
/// </summary>
public class MqttEventInterceptor
{
    /// <summary>
    /// 数值越大越先执行
    /// </summary>
    public int Order = 0;

    /// <summary>
    /// 启动后事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task StartedAsync(EventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 关闭后事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task StoppedAsync(EventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 客户端验证事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task ValidatingConnectionAsync(ValidatingConnectionEventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 客户端连接事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task ClientConnectedAsync(ClientConnectedEventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 客户端断开事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task ClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 订阅主题事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task ClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task ClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 拦截发布的消息事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task InterceptingPublishAsync(InterceptingPublishEventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 未被消费的消息事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public virtual async Task ApplicationMessageNotConsumedAsync(ApplicationMessageNotConsumedEventArgs arg)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 输出日志事件
    /// </summary>
    /// <param name="msg"></param>
    protected static void Logging(string msg)
    {
        if (!App.GetOptions<MqttOptions>().Logging) return;
        Console.WriteLine(msg);
        Log.Information(msg);
    }
}