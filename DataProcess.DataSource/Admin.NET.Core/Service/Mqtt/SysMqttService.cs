// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using MQTTnet.Server;

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统 MQTT 服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 90, Description = "MQTT 服务")]
public class SysMqttService() : IDynamicApiController, ITransient
{
    /// <summary>
    /// 获取客户端列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取客户端列表")]
    public async Task<IList<MqttClientStatus>> GetClients()
    {
        if (MqttHostedService.MqttServer == null)
            throw Oops.Oh("【MQTT】服务未启动");

        return await MqttHostedService.MqttServer.GetClientsAsync();
    }

    /// <summary>
    /// 发布主题消息 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("发布主题消息")]
    public async Task PublicMessage(PublicMessageInput input)
    {
        var mqttHostedService = App.GetRequiredService<MqttHostedService>();
        await mqttHostedService.PublicMessageAsync(input.Topic, input.Message);
    }
}