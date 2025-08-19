// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 系统消息日志表
/// </summary>
[SugarTable(null, "系统消息日志表")]
[SysTable]
[LogTable]
public partial class SysLogMsg : EntityTenant
{
    /// <summary>
    /// 消息标题
    /// </summary>
    [SugarColumn(ColumnDescription = "消息标题", Length = 64)]
    [MaxLength(64)]
    public string Title { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    [SugarColumn(ColumnDescription = "消息内容", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string Message { get; set; }

    /// <summary>
    /// 接收者Id
    /// </summary>
    [SugarColumn(ColumnDescription = "接收者Id")]
    public long ReceiveUserId { get; set; }

    /// <summary>
    /// 接收者名称
    /// </summary>
    [SugarColumn(ColumnDescription = "接收者名称", Length = 32)]
    [MaxLength(32)]
    public string ReceiveUserName { get; set; }

    /// <summary>
    /// 接收者Id集合
    /// </summary>
    [SugarColumn(ColumnDescription = "接收者Id集合", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? ReceiveUserIds { get; set; }

    /// <summary>
    /// 接收者IP
    /// </summary>
    [SugarColumn(ColumnDescription = "接收者IP", Length = 256)]
    [MaxLength(256)]
    public string? ReceiveIp { get; set; }

    /// <summary>
    /// 接收者浏览器
    /// </summary>
    [SugarColumn(ColumnDescription = "接收者浏览器", Length = 128)]
    [MaxLength(128)]
    public string? ReceiveBrowser { get; set; }

    /// <summary>
    /// 接收者操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "接收者操作系统", Length = 128)]
    [MaxLength(128)]
    public string? ReceiveOs { get; set; }

    /// <summary>
    /// 接收者设备
    /// </summary>
    [SugarColumn(ColumnDescription = "接收者设备", Length = 256)]
    [MaxLength(256)]
    public string? ReceiveDevice { get; set; }

    /// <summary>
    /// 发送者Id
    /// </summary>
    [SugarColumn(ColumnDescription = "发送者Id")]
    public long SendUserId { get; set; }

    /// <summary>
    /// 发送者名称
    /// </summary>
    [SugarColumn(ColumnDescription = "发送者名称", Length = 32)]
    [MaxLength(32)]
    public string SendUserName { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [SugarColumn(ColumnDescription = "发送时间")]
    public DateTime SendTime { get; set; }

    /// <summary>
    /// 发送者IP
    /// </summary>
    [SugarColumn(ColumnDescription = "发送者IP", Length = 256)]
    [MaxLength(256)]
    public string? SendIp { get; set; }

    /// <summary>
    /// 发送者浏览器
    /// </summary>
    [SugarColumn(ColumnDescription = "发送者浏览器", Length = 128)]
    [MaxLength(128)]
    public string? SendBrowser { get; set; }

    /// <summary>
    /// 发送者操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "发送者操作系统", Length = 128)]
    [MaxLength(128)]
    public string? SendOs { get; set; }

    /// <summary>
    /// 发送者设备
    /// </summary>
    [SugarColumn(ColumnDescription = "发送者设备", Length = 256)]
    [MaxLength(256)]
    public string? SendDevice { get; set; }
}