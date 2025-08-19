// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 微信支传订单状态变化事件拦截器
/// </summary>
public class WechatPayEventInterceptor
{
    /// <summary>
    /// 数值越大越先执行
    /// </summary>
    public int Order = 0;

    /// <summary>
    /// 信息变化处理器
    /// </summary>
    /// <remarks>主要用来判断 TradeStatus 的状态，这个状态有"空",NOTPAY,SUCCESS, REFUND</remarks>
    /// <param name="oldInfo">旧记录状态</param>
    /// <param name="newInfo">新记录状态</param>
    /// <returns></returns>
    public virtual async Task<bool> PayInforChanged(SysWechatPay oldInfo, SysWechatPay newInfo)
    {
        return await Task.FromResult(true);
    }
}