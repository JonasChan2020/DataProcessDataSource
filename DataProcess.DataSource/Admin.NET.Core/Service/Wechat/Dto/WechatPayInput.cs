// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

public class WechatPayTransactionInput
{
    /// <summary>
    /// OpenId
    /// </summary>
    public string OpenId { get; set; }

    /// <summary>
    /// 订单金额
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 商品描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 附加数据
    /// </summary>
    public string Attachment { get; set; }

    /// <summary>
    /// 优惠标记
    /// </summary>
    public string GoodsTag { get; set; }

    /// <summary>
    /// 关联的商户订单号
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// 关联的商户订单付款状态(或者为第几次支付，有些订单涉及多次支付，比如先付预付款，后补尾款)
    /// </summary>
    public string OrderStatus { get; set; } = "0";

    /// <summary>
    /// 业务标签，用来区分做什么业务
    /// </summary>
    public string Tags { get; set; }

    /// <summary>
    /// 对应业务的主键
    /// </summary>
    public long BusinessId { get; set; }
}

public class WechatPayParaInput
{
    /// <summary>
    /// 订单Id
    /// </summary>
    public string PrepayId { get; set; }
}

public class RefundRequestInput // : WechatTenpayRequest
{
    /// <summary>
    /// 商户订单号(原支付交易对应的付款单号)
    /// </summary>
    public string OutTradeNumber { get; set; }

    ///// <summary>
    ///// 退款单号
    ///// </summary>
    //public string OutRefundNumber { get; set; }
    /// <summary>
    /// 原订单金额(分)
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 退款金额(分)
    /// </summary>
    public int Refund { get; set; }

    /// <summary>
    /// 退款原因
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// 关联的商户订单号
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// 关联的商户订单状态(或者为第几次支付，有些订单涉及多次支付，比如先付预付款，后补尾款)
    /// </summary>
    public string OrderStatus { get; set; }

    /// <summary>
    /// 关联的商户商品编码
    /// </summary>
    public string MerchantGoodsId { get; set; }

    /// <summary>
    /// 关联的商户商品名称
    /// </summary>
    public string GoodsName { get; set; }

    /// <summary>
    /// 关联的商户商品单价
    /// </summary>
    public int UnitPrice { get; set; } = 0;

    /// <summary>
    /// 关联的商户商品退款金额
    /// </summary>
    public int RefundAmount { get; set; } = 0;

    /// <summary>
    /// 关联的商户商品退货数量
    /// </summary>
    public int RefundQuantity { get; set; } = 1;

    /// <summary>
    /// 附加数据
    /// </summary>
    public string Attachment { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }
}

public class PageSysWechatPayInput : BasePageInput
{
    /// <summary>
    /// order_id
    /// </summary>
    /// <example></example>
    public string? OrderId { get; set; }

    /// <summary>
    /// order_status
    /// </summary>
    /// <example></example>
    public string? OrderStatus { get; set; }

    /// <summary>
    /// out_trade_number
    /// </summary>
    /// <example></example>
    public string OutTradeNumber { get; set; }

    /// <summary>
    /// success_time范围
    /// </summary>
    /// <example></example>
    public List<DateTime?> SuccessTimeRange { get; set; }

    /// <summary>
    /// expire_time范围
    /// </summary>
    /// <example></example>
    public List<DateTime?> ExpireTimeRange { get; set; }
}