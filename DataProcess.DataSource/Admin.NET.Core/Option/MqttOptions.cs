// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// MQTT 配置选项
/// </summary>
public sealed class MqttOptions : IConfigurableOptions
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string IPAddress { get; set; }

    /// <summary>
    /// 最大连接数
    /// </summary>
    public int ConnectionBacklog { get; set; }

    /// <summary>
    /// 服务器主动发消息时的ClientId
    /// </summary>
    public string MqttServerId { get; set; }

    /// <summary>
    /// 输出日志
    /// </summary>
    public bool Logging { get; set; }
}