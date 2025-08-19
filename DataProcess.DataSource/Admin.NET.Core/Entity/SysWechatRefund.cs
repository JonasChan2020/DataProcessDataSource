// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 微信退款表
/// </summary>
[SugarTable(null, "系统微信退款表")]
[SysTable]
[SugarIndex("i_{table}_o", nameof(OrderId), OrderByType.Desc)]
public class SysWechatRefund : EntityBase
{
    /// <summary>
    /// 微信支付订单号(原支付交易对应的微信订单号)
    /// </summary>
    [SugarColumn(ColumnDescription = "微信支付订单号", Length = 32)]
    [Required]
    public string TransactionId { get; set; }

    /// <summary>
    /// 商户订单号(原交易对应的商户付款单号)
    /// </summary>
    [SugarColumn(ColumnDescription = "商户付款单号", Length = 32)]
    [Required]
    public string OutTradeNumber { get; set; }

    /// <summary>
    /// 商户系统内部的退款单号，商户系统内部唯一，只能是数字、大小写字母_-|*@ ，同一退款单号多次请求只退一笔。
    /// </summary>
    [SugarColumn(ColumnDescription = "商户退款单号", Length = 64)]
    [Required]
    public string OutRefundNumber { get; set; }

    /// <summary>
    /// 微信接口退款ID
    /// </summary>
    public string RefundId { get; set; }

    /// <summary>
    /// 退款原因，示例：商品已售完
    /// </summary>
    [SugarColumn(ColumnDescription = "退款原因", Length = 80)]
    public string Reason { get; set; }

    /// <summary>
    /// 退款金额
    /// </summary>
    [SugarColumn(ColumnDescription = "退款金额")]
    public int Refund { get; set; }

    /// <summary>
    /// 原订单总金额
    /// </summary>
    [SugarColumn(ColumnDescription = "订单总金额")]
    public int Total { get; set; }

    /// <summary>
    /// 退款结果回调url
    /// </summary>
    [SugarColumn(ColumnDescription = "退款结果回调url", Length = 256)]
    public string? NotifyUrl { get; set; }

    /// <summary>
    /// 退款资金来源, 可不传，默认使用未结算资金退款（仅对老资金流商户适用）
    /// </summary>
    [SugarColumn(ColumnDescription = "退款资金来源", Length = 32)]
    public string? FundsAccount { get; set; }

    /// <summary>
    /// 关联的商户订单号
    /// </summary>
    [SugarColumn(ColumnDescription = "关联的用户订单号", Length = 256)]
    public string? OrderId { get; set; }

    /// <summary>
    /// 关联的商户订单状态(或者为第几次支付，有些订单涉及多次支付，比如先付预付款，后补尾款)
    /// </summary>
    [SugarColumn(ColumnDescription = "关联的商户订单状态", Length = 32)]
    public string? RefundStatus { get; set; }

    /// <summary>
    /// 支完成时间
    /// </summary>
    [SugarColumn(ColumnDescription = "完成时间")]
    public DateTime? SuccessTime { get; set; }

    /// <summary>
    /// 关联的商户商品编码
    /// </summary>
    [SugarColumn(ColumnDescription = "关联的商户商品编码", Length = 32)]
    public string? MerchantGoodsId { get; set; }

    /// <summary>
    /// 关联的商户商品名称
    /// </summary>
    [SugarColumn(ColumnDescription = "关联的商户商品名称", Length = 256)]
    public string? GoodsName { get; set; }

    /// <summary>
    /// 关联的商户商品单价
    /// </summary>
    [SugarColumn(ColumnDescription = "关联的商户商品单价")]
    public int UnitPrice { get; set; }

    /// <summary>
    /// 关联的商户商品退款金额
    /// </summary>
    [SugarColumn(ColumnDescription = "关联的商户商品退款金额")]
    public int RefundAmount { get; set; }

    /// <summary>
    /// 关联的商户商品退货数量
    /// </summary>
    [SugarColumn(ColumnDescription = "关联的商户商品退货数量")]
    public int RefundQuantity { get; set; } = 1;

    /// <summary>
    /// 附加数据
    /// </summary>
    [SugarColumn(ColumnDescription = "附加数据")]
    public string? Attachment { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? Remark { get; set; }
}