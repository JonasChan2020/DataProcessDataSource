// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 微信支付服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 210, Description = "微信支付")]
public class SysWechatPayService : IDynamicApiController, ITransient
{
    private static readonly List<WechatPayEventInterceptor> wechatPayEventHandlers = [];

    /// <summary>
    /// 注册支付记录变化事件处理器
    /// </summary>
    /// <param name="eh">处理器</param>
    /// <param name="order">排序，数据越大越先执行</param>
    public static void AddPayEventInterceptor(WechatPayEventInterceptor eh, int order)
    {
        eh.Order = order;
        wechatPayEventHandlers.Add(eh);
        wechatPayEventHandlers.Sort((a, b) => b.Order - a.Order);
    }

    private readonly SqlSugarRepository<SysWechatPay> _sysWechatPayRep;
    private readonly SqlSugarRepository<SysWechatRefund> _sysWechatRefundRep;
    private readonly WechatPayOptions _wechatPayOptions;
    private readonly PayCallBackOptions _payCallBackOptions;

    private readonly WechatTenpayClient _wechatTenpayClient;

    public SysWechatPayService(SqlSugarRepository<SysWechatPay> sysWechatPayRep,
        SqlSugarRepository<SysWechatRefund> sysWechatRefundRep,
        IOptions<WechatPayOptions> wechatPayOptions,
        IOptions<PayCallBackOptions> payCallBackOptions)
    {
        _sysWechatPayRep = sysWechatPayRep;
        _sysWechatRefundRep = sysWechatRefundRep;
        _wechatPayOptions = wechatPayOptions.Value;
        _payCallBackOptions = payCallBackOptions.Value;

        _wechatTenpayClient = CreateTenpayClient();
    }

    /// <summary>
    /// 初始化微信支付客户端
    /// </summary>
    /// <returns></returns>
    private WechatTenpayClient CreateTenpayClient()
    {
        var cerFilePath = Path.Combine(App.WebHostEnvironment.ContentRootPath, _wechatPayOptions.MerchantCertificatePrivateKey.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (!File.Exists(cerFilePath))
            Log.Warning("商户证书文件不存在:" + cerFilePath);

        var tenpayClientOptions = new WechatTenpayClientOptions()
        {
            MerchantId = _wechatPayOptions.MerchantId,
            MerchantV3Secret = _wechatPayOptions.MerchantV3Secret,
            MerchantCertificateSerialNumber = _wechatPayOptions.MerchantCertificateSerialNumber,
            MerchantCertificatePrivateKey = File.Exists(cerFilePath) ? File.ReadAllText(cerFilePath) : "",
            PlatformCertificateManager = new InMemoryCertificateManager()
        };
        return new WechatTenpayClient(tenpayClientOptions);
    }

    /// <summary>
    /// 生成JSAPI调起支付所需参数 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("生成JSAPI调起支付所需参数")]
    public WechatPayParaOutput GenerateParametersForJsapiPay(WechatPayParaInput input)
    {
        var result = _wechatTenpayClient.GenerateParametersForJsapiPayRequest(_wechatPayOptions.AppId, input.PrepayId);
        return result.Adapt<WechatPayParaOutput>();
    }

    /// <summary>
    /// 微信支付统一下单获取Id(商户直连) 🔖
    /// </summary>
    [DisplayName("微信支付统一下单获取Id(商户直连)")]
    public async Task<CreatePayTransactionOutput> CreatePayTransaction([FromBody] WechatPayTransactionInput input)
    {
        string outTradeNumber = DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff") + (new Random()).Next(100, 1000); // 微信需要的订单号(唯一)

        // 检查订单信息是否已存在(使用“商户交易单号+状态”唯一性判断)
        SysWechatPay wechatPay = null;
        if (!string.IsNullOrEmpty(input.OrderId))
        {
            wechatPay = await _sysWechatPayRep.GetFirstAsync(u => u.OrderId == input.OrderId && u.OrderStatus == input.OrderStatus && input.Total == u.Total);
            if (wechatPay != null)
            {
                outTradeNumber = wechatPay.OutTradeNumber;
            }
        }

        var request = new CreatePayTransactionJsapiRequest()
        {
            OutTradeNumber = outTradeNumber,
            AppId = _wechatPayOptions.AppId,
            Description = input.Description,
            Attachment = input.Attachment,
            GoodsTag = input.GoodsTag,
            ExpireTime = DateTimeOffset.Now.AddMinutes(10),
            NotifyUrl = _payCallBackOptions.WechatPayUrl,
            Amount = new CreatePayTransactionJsapiRequest.Types.Amount() { Total = input.Total },
            Payer = new CreatePayTransactionJsapiRequest.Types.Payer() { OpenId = input.OpenId }
        };
        var response = await _wechatTenpayClient.ExecuteCreatePayTransactionJsapiAsync(request);
        if (!response.IsSuccessful())
            throw Oops.Oh<Exception>(response.ErrorMessage);

        if (wechatPay == null)
        {
            // 保存订单信息
            wechatPay = new SysWechatPay()
            {
                AppId = _wechatPayOptions.AppId,
                MerchantId = _wechatPayOptions.MerchantId,
                OutTradeNumber = request.OutTradeNumber,
                Description = input.Description,
                Attachment = input.Attachment,
                GoodsTag = input.GoodsTag,
                Total = input.Total,
                OpenId = input.OpenId,
                TransactionId = "",
                OrderId = input.OrderId,
                OrderStatus = input.OrderStatus,
                Tags = input.Tags,
                BusinessId = input.BusinessId,
            };
            await _sysWechatPayRep.InsertAsync(wechatPay);
        }

        var singInfo = GenerateParametersForJsapiPay(new WechatPayParaInput() { PrepayId = response.PrepayId });
        return new CreatePayTransactionOutput
        {
            PrepayId = response.PrepayId,
            OutTradeNumber = request.OutTradeNumber,
            SingInfo = singInfo
        };
    }

    /// <summary>
    /// 微信支付统一下单(商户直连)Native 🔖
    /// </summary>
    [DisplayName("微信支付统一下单(商户直连)Native")]
    public async Task<CreatePayTransactionNativeOutput> CreatePayTransactionNative([FromBody] WechatPayTransactionInput input)
    {
        var request = new CreatePayTransactionNativeRequest()
        {
            OutTradeNumber = DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff") + (new Random()).Next(100, 1000), // 微信需要的订单号(唯一)
            AppId = _wechatPayOptions.AppId,
            Description = input.Description,
            Attachment = input.Attachment,
            GoodsTag = input.GoodsTag,
            ExpireTime = DateTimeOffset.Now.AddMinutes(10),
            NotifyUrl = _payCallBackOptions.WechatPayUrl,
            Amount = new CreatePayTransactionNativeRequest.Types.Amount() { Total = input.Total },
            //Payer = new CreatePayTransactionNativeRequest.Types.Payer() { OpenId = input.OpenId }
            Scene = new CreatePayTransactionNativeRequest.Types.Scene() { ClientIp = "127.0.0.1" }
        };
        var response = await _wechatTenpayClient.ExecuteCreatePayTransactionNativeAsync(request);
        if (!response.IsSuccessful())
        {
            JSON.Serialize(response).LogInformation();
            throw Oops.Oh(response.ErrorMessage);
        }
        // 保存订单信息
        var wechatPay = new SysWechatPay()
        {
            AppId = _wechatPayOptions.AppId,
            MerchantId = _wechatPayOptions.MerchantId,
            OutTradeNumber = request.OutTradeNumber,
            Description = input.Description,
            Attachment = input.Attachment,
            GoodsTag = input.GoodsTag,
            Total = input.Total,
            OpenId = input.OpenId,
            TransactionId = "",
            QrcodeContent = response.QrcodeUrl,
            Tags = input.Tags,
            BusinessId = input.BusinessId,
        };
        await _sysWechatPayRep.InsertAsync(wechatPay);
        return new CreatePayTransactionNativeOutput
        {
            OutTradeNumber = request.OutTradeNumber,
            QrcodeUrl = response.QrcodeUrl
        };
    }

    /// <summary>
    /// 微信支付统一下单获取Id(服务商模式) 🔖
    /// </summary>
    [DisplayName("微信支付统一下单获取Id(服务商模式)")]
    public async Task<CreatePayTransactionOutput> CreatePayPartnerTransaction([FromBody] WechatPayTransactionInput input)
    {
        string outTradeNumber = DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff") + (new Random()).Next(100, 1000); // 微信需要的订单号(唯一)

        // 检查订单信息是否已存在(使用“商户交易单号+状态”唯一性判断)
        var wechatPay = await _sysWechatPayRep.GetFirstAsync(u => u.OrderId == input.OrderId && u.OrderStatus == input.OrderStatus);
        if (wechatPay != null)
        {
            outTradeNumber = wechatPay.OutTradeNumber;
        }

        var request = new CreatePayPartnerTransactionJsapiRequest()
        {
            OutTradeNumber = outTradeNumber,
            AppId = _wechatPayOptions.AppId,
            MerchantId = _wechatPayOptions.MerchantId,
            SubAppId = _wechatPayOptions.AppId,
            SubMerchantId = _wechatPayOptions.MerchantId,
            Description = input.Description,
            Attachment = input.Attachment,
            GoodsTag = input.GoodsTag,
            ExpireTime = DateTimeOffset.Now.AddMinutes(10),
            NotifyUrl = _payCallBackOptions.WechatPayUrl,
            Amount = new CreatePayPartnerTransactionJsapiRequest.Types.Amount() { Total = input.Total },
            Payer = new CreatePayPartnerTransactionJsapiRequest.Types.Payer() { OpenId = input.OpenId }
        };
        var response = await _wechatTenpayClient.ExecuteCreatePayPartnerTransactionJsapiAsync(request);
        if (!response.IsSuccessful())
            throw Oops.Oh<Exception>($"JSAPI 下单失败（状态码：{response.GetRawStatus()}，错误代码：{response.ErrorCode}，错误描述：{response.ErrorMessage}）");
        if (wechatPay == null)
        {
            // 保存订单信息
            wechatPay = new SysWechatPay()
            {
                AppId = _wechatPayOptions.AppId,
                MerchantId = _wechatPayOptions.MerchantId,
                SubAppId = _wechatPayOptions.AppId,
                SubMerchantId = _wechatPayOptions.MerchantId,
                OutTradeNumber = request.OutTradeNumber,
                Description = input.Description,
                Attachment = input.Attachment,
                GoodsTag = input.GoodsTag,
                Total = input.Total,
                OpenId = input.OpenId,
                TransactionId = ""
            };
            await _sysWechatPayRep.InsertAsync(wechatPay);
        }

        return new CreatePayTransactionOutput
        {
            PrepayId = response.PrepayId,
            OutTradeNumber = request.OutTradeNumber
        };
    }

    /// <summary>
    /// 获取支付订单详情 🔖
    /// </summary>
    /// <param name="tradeId"></param>
    /// <returns></returns>
    [DisplayName("获取支付订单详情")]
    public async Task<SysWechatPay> GetPayInfo(string tradeId)
    {
        return await _sysWechatPayRep.GetFirstAsync(u => u.OutTradeNumber == tradeId);
    }

    /// <summary>
    /// 获取支付订单详情(微信接口) 🔖
    /// </summary>
    /// <param name="tradeId"></param>
    /// <returns></returns>
    [DisplayName("获取支付订单详情(微信接口)")]
    public async Task<SysWechatPay> GetPayInfoFromWechat(string tradeId)
    {
        var request = new GetPayTransactionByOutTradeNumberRequest();
        request.OutTradeNumber = tradeId;
        var response = await _wechatTenpayClient.ExecuteGetPayTransactionByOutTradeNumberAsync(request);
        // 修改订单支付状态
        var wechatPayOld = await _sysWechatPayRep.GetFirstAsync(u => u.OutTradeNumber == response.OutTradeNumber
            && u.MerchantId == response.MerchantId);
        SysWechatPay wechatPayNew = null;
        if (wechatPayOld != null)
            wechatPayNew = wechatPayOld.DeepCopy();
        // 如果状态不一致就更新数据库中的记录
        if (wechatPayNew != null && wechatPayNew.TradeState != response.TradeState)
        {
            wechatPayNew.OpenId = response.Payer?.OpenId;
            wechatPayNew.TransactionId = response.TransactionId; // 支付订单号
            wechatPayNew.TradeType = response.TradeType; // 交易类型
            wechatPayNew.TradeState = response.TradeState; // 交易状态
            wechatPayNew.TradeStateDescription = response.TradeStateDescription; // 交易状态描述
            wechatPayNew.BankType = response.BankType; // 付款银行类型
            if (response.Amount != null)
            {
                wechatPayNew.Total = response.Amount.Total; // 订单总金额
                wechatPayNew.PayerTotal = response.Amount.PayerTotal; // 用户支付金额
            }
            if (response.SuccessTime.HasValue)
                wechatPayNew.SuccessTime = response.SuccessTime.Value.DateTime; // 支付完成时间
            else
                wechatPayNew.SuccessTime = DateTime.Now;
            await _sysWechatPayRep.AsUpdateable(wechatPayNew).IgnoreColumns(true).ExecuteCommandAsync();
        }
        if (wechatPayOld == null || wechatPayOld.TradeState != wechatPayNew.TradeState)
        {
            // 没必要等所有回调事件处理完再返回给微信，开一个Task执行
            _ = Task.Run(async () =>
            {
                foreach (var eh in wechatPayEventHandlers)
                {
                    try
                    {
                        //这里一定要用 DeepCopy 来创一个新的对象传进不，不然会被外面的 主线程改变就麻烦了
                        if (!await eh.PayInforChanged(wechatPayOld, wechatPayNew.DeepCopy()))
                            break;
                    }
                    catch (Exception ex)
                    {
                        $"GetPayInfoFromWechat 中执行微信支付回调{eh.GetType().Name}出错".LogError(ex);
                    }
                }
            });
        }
        // 下面这里创建一个新的对象，是因为不想把全部字段都返回
        var result = new SysWechatPay()
        {
            AppId = _wechatPayOptions.AppId,
            MerchantId = _wechatPayOptions.MerchantId,
            SubAppId = _wechatPayOptions.AppId,
            SubMerchantId = _wechatPayOptions.MerchantId,
            OutTradeNumber = request.OutTradeNumber,
            Attachment = response.Attachment,
            TransactionId = response.TransactionId,
            TradeType = response.TradeType, // 交易类型
            TradeState = response.TradeState, // 交易状态
            TradeStateDescription = response.TradeStateDescription, // 交易状态描述
            BankType = response.BankType, // 付款银行类型
            SuccessTime = response.SuccessTime.HasValue ? response.SuccessTime.Value.DateTime : DateTime.Now // 支付完成时间
        };

        return result;
    }

    /// <summary>
    /// 微信支付成功回调(商户直连) 🔖
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("微信支付成功回调(商户直连)")]
    public async Task<WechatPayOutput> PayCallBack()
    {
        using var ms = new MemoryStream();
        await App.HttpContext.Request.Body.CopyToAsync(ms);
        var b = ms.ToArray();
        var callbackJson = Encoding.UTF8.GetString(b);

        var callbackModel = _wechatTenpayClient.DeserializeEvent(callbackJson);
        if ("TRANSACTION.SUCCESS".Equals(callbackModel.EventType))
        {
            var callbackResource = _wechatTenpayClient.DecryptEventResource<TransactionResource>(callbackModel);

            // 修改订单支付状态
            var wechatPayOld = await _sysWechatPayRep.GetFirstAsync(u => u.OutTradeNumber == callbackResource.OutTradeNumber
                && u.MerchantId == callbackResource.MerchantId);
            if (wechatPayOld == null) return null;
            var wechatPayNew = wechatPayOld.DeepCopy();
            if (callbackResource.Payer != null)
                wechatPayNew.OpenId = callbackResource.Payer.OpenId; // 支付者标识
            wechatPayNew.TransactionId = callbackResource.TransactionId; // 支付订单号
            wechatPayNew.TradeType = callbackResource.TradeType; // 交易类型
            wechatPayNew.TradeState = callbackResource.TradeState; // 交易状态
            wechatPayNew.TradeStateDescription = callbackResource.TradeStateDescription; // 交易状态描述
            wechatPayNew.BankType = callbackResource.BankType; // 付款银行类型
            wechatPayNew.Total = callbackResource.Amount.Total; // 订单总金额
            wechatPayNew.PayerTotal = callbackResource.Amount.PayerTotal; // 用户支付金额
            wechatPayNew.SuccessTime = callbackResource.SuccessTime.DateTime; // 支付完成时间

            await _sysWechatPayRep.AsUpdateable(wechatPayNew).IgnoreColumns(true).ExecuteCommandAsync();
            // 因为这个是回调给微信的，所以这里没必要等所有回调事件处理完再返回给微信，开一个Task执行
            if (wechatPayOld.TradeState != wechatPayNew.TradeState)
            {
                _ = Task.Run(async () =>
                {
                    foreach (var eh in wechatPayEventHandlers)
                    {
                        try
                        {
                            if (!await eh.PayInforChanged(wechatPayOld, wechatPayNew))
                                break;
                        }
                        catch (Exception ex)
                        {
                            $"PayCallBack 中执行微信支付回调{eh.GetType().Name}出错".LogError(ex);
                        }
                    }
                });
            }
            return new WechatPayOutput()
            {
                Total = wechatPayNew.Total,
                Attachment = wechatPayNew.Attachment,
                GoodsTag = wechatPayNew.GoodsTag,
                OrderId = long.Parse(wechatPayNew.OrderId)
            };
        }

        return null;
    }

    /// <summary>
    /// 微信退款回调(商户直连) 🔖
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("微信退款回调(商户直连)")]
    public async Task<WechatPayOutput> RefundCallBack()
    {
        using var ms = new MemoryStream();
        await App.HttpContext.Request.Body.CopyToAsync(ms);
        var b = ms.ToArray();
        var callbackJson = Encoding.UTF8.GetString(b);

        var callbackModel = _wechatTenpayClient.DeserializeEvent(callbackJson);
        if ("REFUND.SUCCESS".Equals(callbackModel.EventType))
        {
            // 参考：https://pay.weixin.qq.com/docs/merchant/apis/jsapi-payment/refund-result-notice.html
            try
            {
                var callbackRefundResource = _wechatTenpayClient.DecryptEventResource<RefundResource>(callbackModel);
                // 修改订单支付状态
                var wechatRefund = await _sysWechatRefundRep.GetFirstAsync(u => u.OutRefundNumber == callbackRefundResource.OutRefundNumber);
                if (wechatRefund == null) return null;
                wechatRefund.RefundStatus = callbackRefundResource.RefundStatus; // 交易状态
                wechatRefund.SuccessTime = callbackRefundResource.SuccessTime.Value.DateTime; // 支付完成时间

                await _sysWechatRefundRep.AsUpdateable(wechatRefund).IgnoreColumns(true).ExecuteCommandAsync();
                // 有退款，刷新一下订单状态(通过主动查询Wechat接口获取)
                // 通过 GetPayInfoFromWechat 也会触发 WechatPayEventHandler，所以这个回调中不用主动调用
                await GetPayInfoFromWechat(callbackRefundResource.OutTradeNumber);
            }
            catch (Exception ex)
            {
                "微信退款回调时出错：".LogError(ex);
            }
        }
        else
        {
            callbackModel.EventType.LogInformation();
        }

        return null;
    }

    /// <summary>
    /// 微信支付成功回调(服务商模式) 🔖
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("微信支付成功回调(服务商模式)")]
    public async Task PayPartnerCallBack()
    {
        using var ms = new MemoryStream();
        await App.HttpContext.Request.Body.CopyToAsync(ms);
        var b = ms.ToArray();
        var callbackJson = Encoding.UTF8.GetString(b);

        var callbackModel = _wechatTenpayClient.DeserializeEvent(callbackJson);
        if ("TRANSACTION.SUCCESS".Equals(callbackModel.EventType))
        {
            var callbackResource = _wechatTenpayClient.DecryptEventResource<PartnerTransactionResource>(callbackModel);

            // 修改订单支付状态
            var wechatPayOld = await _sysWechatPayRep.GetFirstAsync(u => u.OutTradeNumber == callbackResource.OutTradeNumber
                && u.MerchantId == callbackResource.MerchantId);

            if (wechatPayOld == null) return;
            var wechatPayNew = wechatPayOld.DeepCopy();
            //wechatPay.OpenId = callbackResource.Payer.OpenId; // 支付者标识
            //wechatPay.MerchantId = callbackResource.MerchantId; // 微信商户号
            //wechatPay.OutTradeNumber = callbackResource.OutTradeNumber; // 商户订单号
            wechatPayNew.TransactionId = callbackResource.TransactionId; // 支付订单号
            wechatPayNew.TradeType = callbackResource.TradeType; // 交易类型
            wechatPayNew.TradeState = callbackResource.TradeState; // 交易状态
            wechatPayNew.TradeStateDescription = callbackResource.TradeStateDescription; // 交易状态描述
            wechatPayNew.BankType = callbackResource.BankType; // 付款银行类型
            wechatPayNew.Total = callbackResource.Amount.Total; // 订单总金额
            wechatPayNew.PayerTotal = callbackResource.Amount.PayerTotal; // 用户支付金额
            wechatPayNew.SuccessTime = callbackResource.SuccessTime.DateTime; // 支付完成时间

            await _sysWechatPayRep.AsUpdateable(wechatPayNew).IgnoreColumns(true).ExecuteCommandAsync();
            // 因为这个是回调给微信的，所以这里没必要等所有回调事件处理完再返回给微信，开一个Task执行
            if (wechatPayOld.TradeState != wechatPayNew.TradeState)
            {
                _ = Task.Run(async () =>
                {
                    foreach (var eh in wechatPayEventHandlers)
                    {
                        try
                        {
                            if (!await eh.PayInforChanged(wechatPayOld, wechatPayNew))
                                break;
                        }
                        catch (Exception ex)
                        {
                            $"PayPartnerCallBack 中执行微信支付回调{eh.GetType().Name}出错".LogError(ex);
                        }
                    }
                });
            }
        }
    }

    /// <summary>
    /// 退款申请 🔖
    /// https://pay.weixin.qq.com/docs/merchant/apis/mini-program-payment/create.html
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("微信退款申请)")]
    public async Task Refund(RefundRequestInput input)
    {
        var vechatPay = await _sysWechatPayRep.GetFirstAsync(u => u.OutTradeNumber == input.OutTradeNumber);
        if (vechatPay == null)
            throw Oops.Bah("没有相应支付记录");

        var request = new CreateRefundDomesticRefundRequest()
        {
            OutTradeNumber = input.OutTradeNumber,
            OutRefundNumber = "REFUND_" + DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff"),
            NotifyUrl = _payCallBackOptions.WechatRefundUrl,
            Amount = new CreateRefundDomesticRefundRequest.Types.Amount()
            {
                Total = input.Total,
                Refund = input.Refund
            },
            Reason = input.Reason,
        };
        var response = await _wechatTenpayClient.ExecuteCreateRefundDomesticRefundAsync(request);

        if (!response.IsSuccessful())
        {
            // 退款失败，该单可能已经退款了，所以主动查询微信接口更新状态
            try
            {
                await this.GetPayInfoFromWechat(input.OutTradeNumber);
            }
            catch { }
            throw Oops.Oh<Exception>($"JSAPI 退款申请失败（状态码：{response.GetRawStatus()}，错误代码：{response.ErrorCode}，错误描述：{response.ErrorMessage}）");
        }

        var wechatRefund = await _sysWechatRefundRep.GetFirstAsync(u => u.OutTradeNumber == input.OutTradeNumber);
        if (wechatRefund == null)
        {
            // 保存退款申请信息
            wechatRefund = new SysWechatRefund()
            {
                TransactionId = vechatPay.TransactionId,
                OutTradeNumber = input.OutTradeNumber,
                OutRefundNumber = request.OutRefundNumber, // 每笔付款只退一次，所以这里直接用付款单号
                Reason = request.Reason,
                Refund = input.Refund,
                RefundId = response.RefundId,
                Total = input.Total,
                NotifyUrl = _payCallBackOptions.WechatRefundUrl,
                OrderId = input.OrderId,
                RefundStatus = input.OrderStatus,
                MerchantGoodsId = input.MerchantGoodsId,
                GoodsName = input.GoodsName,
                UnitPrice = input.UnitPrice,
                RefundAmount = input.RefundAmount,
                RefundQuantity = input.RefundQuantity,
                Attachment = input.Attachment,
                Remark = input.Remark
            };
            await _sysWechatRefundRep.InsertAsync(wechatRefund);
            // 发送了退款请求也要更新原定单的状态(从微信查询)
            await this.GetPayInfoFromWechat(input.OutTradeNumber);
        }
    }

    /// <summary>
    /// 查询单笔退款（通过商户退款单号） 🔖
    /// https://pay.weixin.qq.com/docs/merchant/apis/mini-program-payment/query-by-out-refund-no.html
    /// </summary>
    /// <param name="outRefundNumber"></param>
    /// <returns></returns>
    [DisplayName("微信查询单笔退款)")]
    public async Task<GetRefundDomesticRefundByOutRefundNumberResponse> GetRefundByOutRefundNumber(string outRefundNumber)
    {
        var request = new GetRefundDomesticRefundByOutRefundNumberRequest()
        {
            OutRefundNumber = outRefundNumber
        };
        return await _wechatTenpayClient.ExecuteGetRefundDomesticRefundByOutRefundNumberAsync(request);
    }

    /// <summary>
    /// 微信支付订单号查询（校正） 🔖
    /// https://api.mch.weixin.qq.com/v3/pay/transactions/id/{transaction_id}
    /// </summary>
    /// <param name="transactionId"></param>
    /// <returns></returns>
    [DisplayName("微信支付订单号查询（校正）")]
    public async Task<WechatPayOutput> GetPayTransactionByIdAsync(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId))
            throw Oops.Oh("TransactionId 不能为空");

        if (string.IsNullOrEmpty(_wechatPayOptions.MerchantId) || string.IsNullOrEmpty(_wechatPayOptions.MerchantCertificateSerialNumber))
            throw Oops.Oh("商户号或证书序列号不能为空，请检查支付配置");

        var request = new GetPayTransactionByIdRequest()
        {
            MerchantId = _wechatPayOptions.MerchantId,
            TransactionId = transactionId,
            WechatpaySerialNumber = _wechatPayOptions.MerchantCertificateSerialNumber
        };
        var response = await _wechatTenpayClient.ExecuteGetPayTransactionByIdAsync(request);
        if (response.TradeState == "SUCCESS" || response.TradeState == "CLOSED" || response.TradeState == "NOTPAY")
        {
            // 修正订单支付状态
            var wechatPay = await _sysWechatPayRep.GetFirstAsync(u => u.TransactionId == request.TransactionId && u.MerchantId == request.MerchantId);
            if (wechatPay != null && string.IsNullOrEmpty(wechatPay.TradeState))
            {
                wechatPay.TradeType = response.TradeType; // 交易类型
                wechatPay.TradeState = response.TradeState; // 交易状态
                wechatPay.TradeStateDescription = response.TradeStateDescription; // 交易状态描述
                wechatPay.OpenId = response.Payer?.OpenId;// 付款用户OpenId
                wechatPay.BankType = response.BankType; // 付款银行类型
                wechatPay.PayerTotal = response.Amount?.PayerTotal; // 用户支付金额
                wechatPay.SuccessTime = response.SuccessTime?.DateTime; // 支付完成时间
                await _sysWechatPayRep.AsUpdateable(wechatPay).IgnoreColumns(true).ExecuteCommandAsync();
                return wechatPay.Adapt<WechatPayOutput>();
            }
        }
        return response.Adapt<WechatPayOutput>();
    }

    /// <summary>
    /// 商户订单号查询（校正） 🔖
    ///  https://api.mch.weixin.qq.com/v3/pay/transactions/out-trade-no/{out_trade_no}
    /// </summary>
    /// <param name="outTradeNumber"></param>
    /// <returns></returns>
    [DisplayName("微信商户订单号查询（校正）")]
    public async Task<WechatPayOutput> GetPayTransactionByOutTradeNumberAsync(string outTradeNumber)
    {
        if (string.IsNullOrEmpty(outTradeNumber))
            throw Oops.Oh("商户订单号(OutTradeNumber)不能为空");

        if (string.IsNullOrEmpty(_wechatPayOptions.MerchantId) || string.IsNullOrEmpty(_wechatPayOptions.MerchantCertificateSerialNumber))
            throw Oops.Oh("商户号或证书序列号不能为空，请检查支付配置");

        var request = new GetPayTransactionByOutTradeNumberRequest()
        {
            MerchantId = _wechatPayOptions.MerchantId,
            OutTradeNumber = outTradeNumber,
            WechatpaySerialNumber = _wechatPayOptions.MerchantCertificateSerialNumber,
        };
        var response = await _wechatTenpayClient.ExecuteGetPayTransactionByOutTradeNumberAsync(request);
        if (response.TradeState == "SUCCESS" || response.TradeState == "CLOSED" || response.TradeState == "NOTPAY")
        {
            // 修正订单支付状态
            var wechatPay = await _sysWechatPayRep.GetFirstAsync(u => u.OutTradeNumber == outTradeNumber && u.MerchantId == request.MerchantId);
            if (wechatPay != null && string.IsNullOrEmpty(wechatPay.TradeState))
            {
                wechatPay.TransactionId = response.TransactionId; // 支付订单号
                wechatPay.TradeType = response.TradeType; // 交易类型
                wechatPay.TradeState = response.TradeState; // 交易状态
                wechatPay.TradeStateDescription = response.TradeStateDescription; // 交易状态描述
                wechatPay.OpenId = response.Payer?.OpenId;// 付款用户OpenId
                wechatPay.BankType = response.BankType; // 付款银行类型
                wechatPay.PayerTotal = response.Amount?.PayerTotal; // 用户支付金额
                wechatPay.SuccessTime = response.SuccessTime?.DateTime; // 支付完成时间
                await _sysWechatPayRep.AsUpdateable(wechatPay).IgnoreColumns(true).ExecuteCommandAsync();
                return wechatPay.Adapt<WechatPayOutput>();
            }
        }
        return response.Adapt<WechatPayOutput>();
    }

    /// <summary>
    /// 获取支付记录分页列表 🔖
    /// </summary>
    /// <param name="input">PageSysWechatPayInput</param>
    /// <returns></returns>
    [DisplayName("获取支付记录分页列表")]
    public async Task<SqlSugarPagedList<SysWechatPay>> PageAsync(PageSysWechatPayInput input)
    {
        var query = _sysWechatPayRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.OrderId), u => u.OrderId == input.OrderId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.OrderStatus), u => u.OrderStatus == input.OrderStatus)
            .WhereIF(!string.IsNullOrWhiteSpace(input.OutTradeNumber), u => u.OutTradeNumber.Contains(input.OutTradeNumber.Trim()));

        if (input.SuccessTimeRange != null && input.SuccessTimeRange.Count > 0)
        {
            DateTime? start = input.SuccessTimeRange[0];
            query.WhereIF(start.HasValue, u => u.SuccessTime > start);
            if (input.SuccessTimeRange.Count > 1 && input.SuccessTimeRange[1].HasValue)
            {
                var end = input.SuccessTimeRange[1].Value.AddDays(1);
                query.Where(u => u.SuccessTime < end);
            }
        }
        if (input.ExpireTimeRange != null && input.ExpireTimeRange.Count > 0)
        {
            DateTime? start = input.ExpireTimeRange[0];
            query.WhereIF(start.HasValue, u => u.ExpireTime > start);
            if (input.ExpireTimeRange.Count > 1 && input.ExpireTimeRange[1].HasValue)
            {
                var end = input.ExpireTimeRange[1].Value.AddDays(1);
                query.Where(u => u.ExpireTime < end);
            }
        }
        query.OrderByDescending(u => u.CreateTime);
        return await query.ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 根据支付Id获取退款信息列表 🔖
    /// </summary>
    /// <param name="transactionId"></param>
    /// <param name="outTradeNumber"></param>
    /// <returns></returns>
    [DisplayName("根据支付Id获取退款信息列表")]
    public async Task<List<SysWechatRefund>> GetRefundList([FromQuery] string transactionId, [FromQuery] string outTradeNumber)
    {
        return await _sysWechatRefundRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(transactionId), u => u.TransactionId == transactionId)
            .WhereIF(!string.IsNullOrEmpty(outTradeNumber), u => u.OutTradeNumber == outTradeNumber)
            .ToListAsync();
    }
}