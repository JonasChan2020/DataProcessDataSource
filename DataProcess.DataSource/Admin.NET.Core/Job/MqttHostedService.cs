// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// MQTT 服务
/// </summary>
public class MqttHostedService(IOptions<MqttOptions> mqttOptions) : IHostedService, ISingleton
{
    private readonly MqttOptions _mqttOptions = mqttOptions.Value;
    public static MqttServer MqttServer { get; set; }
    public static readonly List<MqttEventInterceptor> MqttEventInterceptors = []; // MQTT 事件拦截器集合

    /// <summary>
    /// 注册 MQTT 事件拦截器
    /// </summary>
    /// <param name="mqttEventInterceptor"></param>
    /// <param name="order"></param>
    public static void AddMqttEventInterceptor(MqttEventInterceptor mqttEventInterceptor, int order = 0)
    {
        mqttEventInterceptor.Order = order;
        MqttEventInterceptors.Add(mqttEventInterceptor);
        MqttEventInterceptors.Sort((a, b) => b.Order - a.Order);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_mqttOptions.Enabled) return;

        // 注册 MQTT 自定义客户端验证事件拦截器
        AddMqttEventInterceptor(new DefaultMqttEventInterceptor());

        var options = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint() // 默认地址127.0.0.1
            .WithDefaultEndpointPort(_mqttOptions.Port) // 端口号
                                                        //.WithDefaultEndpointBoundIPAddress(_mqttOptions.IPAddress) // IP地址
            .WithConnectionBacklog(_mqttOptions.ConnectionBacklog) // 最大连接数
            .WithPersistentSessions()
            .Build();

        MqttServer = new MqttServerFactory().CreateMqttServer(options);
        MqttServer.StartedAsync += MqttServer_StartedAsync; // 启动后事件
        MqttServer.StoppedAsync += MqttServer_StoppedAsync; // 关闭后事件

        MqttServer.ValidatingConnectionAsync += MqttServer_ValidatingConnectionAsync;        // 客户端验证事件
        MqttServer.ClientConnectedAsync += MqttServer_ClientConnectedAsync;                  // 客户端连接事件
        MqttServer.ClientDisconnectedAsync += MqttServer_ClientDisconnectedAsync;            // 客户端断开事件

        MqttServer.ClientSubscribedTopicAsync += MqttServer_ClientSubscribedTopicAsync;                 // 订阅主题事件
        MqttServer.ClientUnsubscribedTopicAsync += MqttServer_ClientUnsubscribedTopicAsync;             // 取消订阅事件
        MqttServer.InterceptingPublishAsync += MqttServer_InterceptingPublishAsync;                     // 拦截接收消息
        MqttServer.ApplicationMessageNotConsumedAsync += MqttServer_ApplicationMessageNotConsumedAsync; // 消息未被消费

        await MqttServer.StartAsync();
    }

    /// <summary>
    /// 启动后事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private Task MqttServer_StartedAsync(EventArgs arg)
    {
        Console.WriteLine($"【MQTT】服务已启动，端口：{_mqttOptions.Port}...... {DateTime.Now}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 关闭后事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private async Task MqttServer_StoppedAsync(EventArgs arg)
    {
        Console.WriteLine($"【MQTT】服务已关闭...... {DateTime.Now}");
        foreach (var eh in MqttEventInterceptors)
        {
            await eh.StoppedAsync(arg);
        }
    }

    /// <summary>
    /// 客户端验证事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private async Task MqttServer_ValidatingConnectionAsync(ValidatingConnectionEventArgs arg)
    {
        foreach (var eh in MqttEventInterceptors)
        {
            await eh.ValidatingConnectionAsync(arg);
            if (arg.ReasonCode != MqttConnectReasonCode.Success)
                break;
        }
    }

    /// <summary>
    /// 客户端连接事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private async Task MqttServer_ClientConnectedAsync(ClientConnectedEventArgs arg)
    {
        foreach (var eh in MqttEventInterceptors)
        {
            await eh.ClientConnectedAsync(arg);
        }

        Logging($"客户端连接：客户端ID=【{arg.ClientId}】已连接：用户名=【{arg.UserName}】地址=【{arg.RemoteEndPoint}】 {DateTime.Now}");
    }

    /// <summary>
    /// 客户端断开事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task MqttServer_ClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
    {
        foreach (var eh in MqttEventInterceptors)
        {
            await eh.ClientDisconnectedAsync(arg);
        }

        Logging($"客户端断开：客户端ID=【{arg.ClientId}】已断开：用户名=【{arg.UserName}】地址=【{arg.RemoteEndPoint}】 {DateTime.Now}");
    }

    /// <summary>
    /// 订阅主题事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private async Task MqttServer_ClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
    {
        foreach (var eh in MqttEventInterceptors)
        {
            await eh.ClientSubscribedTopicAsync(arg);
        }

        Logging($"订阅主题：客户端ID=【{arg.ClientId}】订阅主题=【{arg.TopicFilter}】 {DateTime.Now}");
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private async Task MqttServer_ClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs arg)
    {
        foreach (var eh in MqttEventInterceptors)
        {
            await eh.ClientUnsubscribedTopicAsync(arg);
        }

        Logging($"取消订阅：客户端ID=【{arg.ClientId}】取消订阅主题=【{arg.TopicFilter}】 {DateTime.Now}");
    }

    /// <summary>
    /// 拦截发布的消息事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private async Task MqttServer_InterceptingPublishAsync(InterceptingPublishEventArgs arg)
    {
        if (string.Equals(arg.ClientId, _mqttOptions.MqttServerId))
            return;

        foreach (var eh in MqttEventInterceptors)
        {
            await eh.InterceptingPublishAsync(arg);
        }

        Logging($"拦截消息：客户端ID=【{arg.ClientId}】 Topic主题=【{arg.ApplicationMessage.Topic}】 消息=【{Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}】 qos等级=【{arg.ApplicationMessage.QualityOfServiceLevel}】 {DateTime.Now}");
    }

    /// <summary>
    /// 未被消费的消息事件
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private async Task MqttServer_ApplicationMessageNotConsumedAsync(ApplicationMessageNotConsumedEventArgs arg)
    {
        foreach (var eh in MqttEventInterceptors)
        {
            await eh.ApplicationMessageNotConsumedAsync(arg);
        }

        Logging($"接收消息：发送端ID=【{arg.SenderId}】 Topic主题=【{arg.ApplicationMessage.Topic}】 消息=【{Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}】 qos等级=【{arg.ApplicationMessage.QualityOfServiceLevel}】 {DateTime.Now}");
    }

    /// <summary>
    /// 发布主题消息
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="message"></param>
    public async Task PublicMessageAsync(string topic, string message)
    {
        var applicationMessage = new MqttApplicationMessageBuilder()
           .WithTopic(topic)
           .WithPayload(message)
           .Build();

        await MqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(applicationMessage)
        {
            SenderClientId = _mqttOptions.MqttServerId,
            SenderUserName = _mqttOptions.MqttServerId,
        });

        Logging($"服务器发布主题：{topic}, 内容：{message}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 输出日志
    /// </summary>
    /// <param name="msg"></param>
    protected void Logging(string msg)
    {
        if (!_mqttOptions.Logging) return;
        Console.WriteLine(msg);
        Log.Information(msg);
    }
}