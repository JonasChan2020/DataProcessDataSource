// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Furion.Shapeless;

namespace Admin.NET.Core;

/// <summary>
/// 数据库日志写入器
/// </summary>
public class DatabaseLoggingWriter : IDatabaseLoggingWriter, IDisposable
{
    private readonly IServiceScope _serviceScope;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<DatabaseLoggingWriter> _logger;
    private readonly SysConfigService _sysConfigService;
    private readonly SqlSugarScopeProvider _db;

    public DatabaseLoggingWriter(IServiceScopeFactory serviceScopeFactory, IEventPublisher eventPublisher, ILogger<DatabaseLoggingWriter> logger)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _sysConfigService = _serviceScope.ServiceProvider.GetRequiredService<SysConfigService>();
        _eventPublisher = eventPublisher;
        _logger = logger;

        // 切换日志独立数据库
        _db = SqlSugarSetup.ITenant.IsAnyConnection(SqlSugarConst.LogConfigId)
            ? SqlSugarSetup.ITenant.GetConnectionScope(SqlSugarConst.LogConfigId)
            : SqlSugarSetup.ITenant.GetConnectionScope(SqlSugarConst.MainConfigId);
    }

    /// <summary>
    /// 根据操作日志内容解析出控制器名称和函数名称
    /// </summary>
    /// <param name="logText"></param>
    /// <returns></returns>
    private static (string actionName, string controllerName) ExtractActionAndController(string logText)
    {
        try
        {
            var targetLine = logText.Split('\n')[1]; // 获取第二行
            var parts = targetLine.Split('.'); // 按点号分割字符串
            // 获取最后两个部分
            if (parts.Length >= 2)
            {
                var actionName = parts[^1].Split('(')[0].Trim(); // 移除方法参数部分
                var controllerName = parts[^2].Trim(); // 获取Controller名称
                return (actionName, controllerName);
            }
            return (string.Empty, string.Empty);
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }

    public async Task WriteAsync(LogMessage logMsg, bool flush)
    {
        var jsonStr = logMsg.Context?.Get("loggingMonitor")?.ToString();
        if (string.IsNullOrWhiteSpace(jsonStr))
        {
            var title = logMsg.Context.Get("Title")?.ToString() ?? "自定义操作日志";
            var actionName = logMsg.Context.Get("Action")?.ToString() ?? "";
            var controllerName = logMsg.Context.Get("Controller")?.ToString() ?? "";
            var url = logMsg.Context.Get("Url")?.ToString() ?? "";
            var method = logMsg.Context.Get("Method")?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(actionName) && string.IsNullOrWhiteSpace(controllerName))
            {
                // 从日志内容中获取控制器名称和函数名称
                var (action, controller) = ExtractActionAndController(logMsg.Message);
                actionName = action;
                controllerName = controller;
            }
            await _db.Insertable(new SysLogOp
            {
                DisplayTitle = title,
                LogDateTime = logMsg.LogDateTime,
                ActionName = actionName,
                ControllerName = controllerName,
                EventId = logMsg.EventId.Id,
                ThreadId = logMsg.ThreadId,
                TraceId = logMsg.TraceId,
                Exception = logMsg.Exception == null ? null : JSON.Serialize(logMsg.Exception),
                Message = logMsg.Message,
                LogLevel = logMsg.LogLevel,
                HttpMethod = method,
                RequestUrl = url,
                Status = "200",
            }).ExecuteCommandAsync();
            return;
        }

        var loggingMonitor = JSON.Deserialize<dynamic>(jsonStr);
        // 获取当前操作者
        string account = "", realName = "", userId = "", tenantId = "";
        if (loggingMonitor.authorizationClaims != null)
        {
            var authDict = (loggingMonitor.authorizationClaims as IEnumerable<dynamic>)!.ToDictionary(u => u.type.ToString(), u => u.value.ToString());
            account = authDict?.GetValueOrDefault(ClaimConst.Account);
            realName = authDict?.GetValueOrDefault(ClaimConst.RealName);
            tenantId = authDict?.GetValueOrDefault(ClaimConst.TenantId);
            userId = authDict?.GetValueOrDefault(ClaimConst.UserId);
        }

        // 优先获取 X-Forwarded-For 头部信息携带的IP地址（如nginx代理配置转发）
        var remoteIPv4 = ((JArray)loggingMonitor.requestHeaders).OfType<JObject>()
            .FirstOrDefault(header => (string)header["key"] == "X-Forwarded-For")?["value"]?.ToString();

        // 获取IP地理位置
        if (string.IsNullOrEmpty(remoteIPv4))
            remoteIPv4 = loggingMonitor.remoteIPv4;
        (string ipLocation, double? longitude, double? latitude) = CommonHelper.GetIpAddress(remoteIPv4);

        // 获取设备信息
        var browser = "";
        var os = "";
        if (loggingMonitor.userAgent != null)
        {
            var client = Parser.GetDefault().Parse(loggingMonitor.userAgent.ToString());
            browser = $"{client.UA.Family} {client.UA.Major}.{client.UA.Minor} / {client.Device.Family}";
            os = $"{client.OS.Family} {client.OS.Major} {client.OS.Minor}";
        }

        // 捕捉异常，否则会由于 unhandled exception 导致程序崩溃
        try
        {
            // 记录异常日志-发送邮件
            if (logMsg.Exception != null || loggingMonitor.exception != null)
            {
                await _db.Insertable(new SysLogEx
                {
                    ControllerName = loggingMonitor.controllerName,
                    ActionName = loggingMonitor.actionTypeName,
                    DisplayTitle = loggingMonitor.displayTitle,
                    Status = loggingMonitor.returnInformation?.httpStatusCode,
                    RemoteIp = remoteIPv4,
                    Location = ipLocation,
                    Longitude = longitude,
                    Latitude = latitude,
                    Browser = browser, // loggingMonitor.userAgent,
                    Os = os, // loggingMonitor.osDescription + " " + loggingMonitor.osArchitecture,
                    Elapsed = loggingMonitor.timeOperationElapsedMilliseconds,
                    LogDateTime = logMsg.LogDateTime,
                    Account = account,
                    RealName = realName,
                    HttpMethod = loggingMonitor.httpMethod,
                    RequestUrl = loggingMonitor.requestUrl,
                    RequestParam = (loggingMonitor.parameters == null || loggingMonitor.parameters.Count == 0) ? null : JSON.Serialize(loggingMonitor.parameters[0].value),
                    ReturnResult = loggingMonitor.returnInformation == null ? null : JSON.Serialize(loggingMonitor.returnInformation),
                    EventId = logMsg.EventId.Id,
                    ThreadId = logMsg.ThreadId,
                    TraceId = logMsg.TraceId,
                    Exception = JSON.Serialize(loggingMonitor.exception),
                    Message = logMsg.Message,
                    CreateUserId = string.IsNullOrWhiteSpace(userId) ? 0 : long.Parse(userId),
                    TenantId = string.IsNullOrWhiteSpace(tenantId) ? 0 : long.Parse(tenantId),
                    LogLevel = logMsg.LogLevel
                }).ExecuteCommandAsync();

                // 将异常日志发送到邮件
                await _eventPublisher.PublishAsync(CommonConst.SendErrorMail, logMsg.Exception ?? loggingMonitor.exception);

                return;
            }

            // 记录访问日志-登录退出
            if (loggingMonitor.actionName == "login" || loggingMonitor.actionName == "loginPhone" || loggingMonitor.actionName == "logout")
            {
                if (loggingMonitor.actionName != "logout")
                {
                    dynamic para = Clay.Parse((loggingMonitor.parameters == null) ? null : JSON.Serialize(loggingMonitor.parameters[0].value));
                    if (loggingMonitor.actionName == "login")
                        account = para.account;
                    else if (loggingMonitor.actionName == "loginPhone")
                        account = para.phone;
                }

                await _db.Insertable(new SysLogVis
                {
                    ControllerName = loggingMonitor.displayName,
                    ActionName = loggingMonitor.actionTypeName,
                    DisplayTitle = loggingMonitor.displayTitle,
                    Status = loggingMonitor.returnInformation?.httpStatusCode,
                    RemoteIp = remoteIPv4,
                    Location = ipLocation,
                    Longitude = longitude,
                    Latitude = latitude,
                    Browser = browser, // loggingMonitor.userAgent,
                    Os = os, // loggingMonitor.osDescription + " " + loggingMonitor.osArchitecture,
                    Elapsed = loggingMonitor.timeOperationElapsedMilliseconds,
                    LogDateTime = logMsg.LogDateTime,
                    Account = account,
                    RealName = realName,
                    CreateUserId = string.IsNullOrWhiteSpace(userId) ? 0 : long.Parse(userId),
                    TenantId = string.IsNullOrWhiteSpace(tenantId) ? 0 : long.Parse(tenantId),
                    LogLevel = logMsg.LogLevel
                }).ExecuteCommandAsync();
                return;
            }

            // 记录操作日志
            if (!await _sysConfigService.GetConfigValueByCode<bool>(ConfigConst.SysOpLog)) return;
            await _db.Insertable(new SysLogOp
            {
                ControllerName = loggingMonitor.controllerName,
                ActionName = loggingMonitor.actionTypeName,
                DisplayTitle = loggingMonitor.displayTitle,
                Status = loggingMonitor.returnInformation?.httpStatusCode,
                RemoteIp = remoteIPv4,
                Location = ipLocation,
                Longitude = longitude,
                Latitude = latitude,
                Browser = browser, // loggingMonitor.userAgent,
                Os = os, // loggingMonitor.osDescription + " " + loggingMonitor.osArchitecture,
                Elapsed = loggingMonitor.timeOperationElapsedMilliseconds,
                LogDateTime = logMsg.LogDateTime,
                Account = account,
                RealName = realName,
                HttpMethod = loggingMonitor.httpMethod,
                RequestUrl = loggingMonitor.requestUrl,
                RequestParam = (loggingMonitor.parameters == null || loggingMonitor.parameters.Count == 0) ? null : JSON.Serialize(loggingMonitor.parameters[0].value),
                ReturnResult = loggingMonitor.returnInformation == null ? null : JSON.Serialize(loggingMonitor.returnInformation),
                EventId = logMsg.EventId.Id,
                ThreadId = logMsg.ThreadId,
                TraceId = logMsg.TraceId,
                Exception = loggingMonitor.exception == null ? null : JSON.Serialize(loggingMonitor.exception),
                Message = logMsg.Message,
                CreateUserId = string.IsNullOrWhiteSpace(userId) ? 0 : long.Parse(userId),
                TenantId = string.IsNullOrWhiteSpace(tenantId) ? 0 : long.Parse(tenantId),
                LogLevel = logMsg.LogLevel
            }).ExecuteCommandAsync();

            await Task.Delay(50); // 延迟 0.05 秒写入数据库，有效减少高频写入数据库导致死锁问题
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "操作日志入库");
        }
    }

    /// <summary>
    /// 释放服务作用域
    /// </summary>
    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}