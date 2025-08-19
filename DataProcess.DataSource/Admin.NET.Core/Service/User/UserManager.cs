// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 当前登录用户信息
/// </summary>
public class UserManager : IScoped
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 应用Id
    /// </summary>
    public long AppId => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.AppId)?.Value).ToLong();

    /// <summary>
    /// 租户Id
    /// </summary>
    public long TenantId => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.TenantId)?.Value).ToLong();

    /// <summary>
    /// 用户Id
    /// </summary>
    public long UserId => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.UserId)?.Value).ToLong();

    /// <summary>
    /// 用户账号
    /// </summary>
    public string Account => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.Account)?.Value;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.RealName)?.Value;

    /// <summary>
    /// 昵称
    /// </summary>
    public string NickName => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.NickName)?.Value;

    /// <summary>
    /// 账号类型
    /// </summary>
    public AccountTypeEnum? AccountType => int.TryParse(_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.AccountType)?.Value, out var val) ? (AccountTypeEnum?)val : null;

    /// <summary>
    /// 是否超级管理员
    /// </summary>
    public bool SuperAdmin => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.AccountType)?.Value == ((int)AccountTypeEnum.SuperAdmin).ToString();

    /// <summary>
    /// 是否系统管理员
    /// </summary>
    public bool SysAdmin => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.AccountType)?.Value == ((int)AccountTypeEnum.SysAdmin).ToString();

    /// <summary>
    /// 组织机构Id
    /// </summary>
    public long OrgId => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.OrgId)?.Value).ToLong();

    /// <summary>
    /// 组织机构名称
    /// </summary>
    public string OrgName => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.OrgName)?.Value);

    /// <summary>
    /// 组织机构Id
    /// </summary>
    public string OrgType => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.OrgType)?.Value);

    /// <summary>
    /// 组织机构级别
    /// </summary>
    public int OrgLevel => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.OrgLevel)?.Value).ToInt();

    /// <summary>
    /// 登录模式
    /// </summary>
    public int LoginMode => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.LoginMode)?.Value).ToInt();

    /// <summary>
    /// Token版本号
    /// </summary>
    public int TokenVersion => (_httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.TokenVersion)?.Value).ToInt();

    /// <summary>
    /// 微信OpenId
    /// </summary>
    public string OpenId => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimConst.OpenId)?.Value;

    public UserManager(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
}