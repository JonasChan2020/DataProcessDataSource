// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Admin.NET.Core.ApiKeyAuth;

/// <summary>
/// ApiKey 身份验证处理
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public const string AuthenticationScheme = "ApiKey";

    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKey = Request.Headers["X-API-KEY"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return await AuthenticateResultFailAsync("API-KEY 不能为空");

        var repAcs = App.GetRequiredService<SqlSugarRepository<SysOpenAccess>>();
        var acsInfo = await repAcs.AsQueryable().Includes(u => u.BindUser).Includes(u => u.BindUser, p => p.SysOrg).FirstAsync(c => c.AccessSecret == apiKey);
        if (acsInfo == null)
            return await AuthenticateResultFailAsync("API-KEY 无效");

        var identity = new ClaimsIdentity(AuthenticationScheme);
        identity.AddClaims(
        [
            new Claim(ClaimConst.UserId, acsInfo.BindUserId + ""),
            new Claim(ClaimConst.TenantId, acsInfo.BindTenantId + ""),
            new Claim(ClaimConst.Account, acsInfo.BindUser.Account + ""),
            new Claim(ClaimConst.RealName, acsInfo.BindUser.RealName),
            new Claim(ClaimConst.AccountType, ((int)acsInfo.BindUser.AccountType).ToString()),
            new Claim(ClaimConst.OrgId, acsInfo.BindUser.OrgId + ""),
            new Claim(ClaimConst.OrgName, acsInfo.BindUser.SysOrg?.Name + ""),
            new Claim(ClaimConst.OrgType, acsInfo.BindUser.SysOrg?.Type + ""),
            new Claim(ClaimConst.TokenVersion,"1")
        ]);
        var user = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(user, AuthenticationScheme));
    }

    private Task<AuthenticateResult> AuthenticateResultFailAsync(string message)
    {
        // 写入身份验证失败消息
        Context.Items[SignatureAuthenticationDefaults.AuthenticateFailMsgKey] = message;
        return Task.FromResult(AuthenticateResult.Fail(message));
    }
}