// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Furion.DataEncryption;
using Furion.DataValidation;
using Lazy.Captcha.Core;
using Microsoft.AspNetCore.Http;
using Yitter.IdGenerator;

namespace Admin.NET.Application.Service.App;

/// <summary>
/// 移动应用服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 500, Description = "移动应用")]
public class AppAuthService : IDynamicApiController, ITransient
{
    private readonly AppUserManager _appUserManager;
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SysRoleService _sysRoleService;
    private readonly SysOnlineUserService _sysOnlineUserService;
    private readonly SysConfigService _sysConfigService;
    private readonly ICaptcha _captcha;
    private readonly SysCacheService _sysCacheService;

    public AppAuthService(AppUserManager appUserManager,
        SqlSugarRepository<SysUser> sysUserRep,
        IHttpContextAccessor httpContextAccessor,
        SysRoleService sysRoleService,
        SysOnlineUserService sysOnlineUserService,
        SysConfigService sysConfigService,
        ICaptcha captcha,
        SysCacheService sysCacheService)
    {
        _appUserManager = appUserManager;
        _sysUserRep = sysUserRep;
        _httpContextAccessor = httpContextAccessor;
        _sysRoleService = sysRoleService;
        _sysOnlineUserService = sysOnlineUserService;
        _sysConfigService = sysConfigService;
        _captcha = captcha;
        _sysCacheService = sysCacheService;
    }

    /// <summary>
    /// 账号密码登录 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("账号密码登录")]
    public virtual async Task<LoginOutput> Login([Required] LoginInput input)
    {
        // 判断密码错误次数（缓存30分钟）
        var keyPasswordErrorTimes = $"{CacheConst.KeyPasswordErrorTimes}{input.Account}";
        var passwordErrorTimes = _sysCacheService.Get<int>(keyPasswordErrorTimes);
        var passwdMaxErrorTimes = await _sysConfigService.GetConfigValueByCode<int>(ConfigConst.SysPasswordMaxErrorTimes);
        if (passwordErrorTimes >= passwdMaxErrorTimes)
            throw Oops.Oh(ErrorCodeEnum.D1027);

        // 判断是否开启验证码并校验
        input.TenantId = input.TenantId <= 0 ? SqlSugarConst.DefaultTenantId : input.TenantId;
        var tenant = _sysCacheService.Get<List<SysTenant>>(CacheConst.KeyTenant)?.FirstOrDefault(u => u.Id == input.TenantId);
        if (tenant == null)
        {
            await Furion.App.GetRequiredService<SysTenantService>().CacheTenant(); // 重新生成租户列表缓存
            tenant = _sysCacheService.Get<List<SysTenant>>(CacheConst.KeyTenant)?.FirstOrDefault(u => u.Id == input.TenantId);
            if (tenant == null) throw Oops.Oh(ErrorCodeEnum.D0007);
        }
        if (tenant.Captcha == true && !_captcha.Validate(input.CodeId.ToString(), input.Code))
            throw Oops.Oh(ErrorCodeEnum.D0008);

        // 租户是否被禁用
        if (tenant != null && tenant.Status == StatusEnum.Disable)
            throw Oops.Oh(ErrorCodeEnum.Z1003);

        // 账号是否存在
        var user = await _sysUserRep.AsQueryable().Includes(t => t.SysOrg).ClearFilter().FirstAsync(u => u.Account.Equals(input.Account));
        _ = user ?? throw Oops.Oh(ErrorCodeEnum.D0009);

        // 账号是否被冻结
        if (user.Status == StatusEnum.Disable)
            throw Oops.Oh(ErrorCodeEnum.D1017);

        // 国密SM2解密（前端密码传输SM2加密后的）
        try
        {
            input.Password = CryptogramHelper.SM2Decrypt(input.Password);
        }
        catch
        {
            throw Oops.Oh(ErrorCodeEnum.D0010);
        }

        VerifyPassword(input, keyPasswordErrorTimes, passwordErrorTimes, user);

        // 登录成功则清空密码错误次数
        _sysCacheService.Remove(keyPasswordErrorTimes);

        return await CreateToken(user);
    }

    /// <summary>
    /// 验证用户密码
    /// </summary>
    /// <param name="input"></param>
    /// <param name="keyErrorPasswordCount"></param>
    /// <param name="errorPasswordCount"></param>
    /// <param name="user"></param>
    private void VerifyPassword(LoginInput input, string keyErrorPasswordCount, int errorPasswordCount, SysUser user)
    {
        if (CryptogramHelper.CryptoType == CryptogramEnum.MD5.ToString())
        {
            if (!user.Password.Equals(MD5Encryption.Encrypt(input.Password)))
            {
                _sysCacheService.Set(keyErrorPasswordCount, ++errorPasswordCount, TimeSpan.FromMinutes(30));
                throw Oops.Oh(ErrorCodeEnum.D1000);
            }
        }
        else
        {
            if (!CryptogramHelper.Decrypt(user.Password).Equals(input.Password))
            {
                _sysCacheService.Set(keyErrorPasswordCount, ++errorPasswordCount, TimeSpan.FromMinutes(30));
                throw Oops.Oh(ErrorCodeEnum.D1000);
            }
        }
    }

    /// <summary>
    /// 手机号登录 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("手机号登录")]
    public virtual async Task<LoginOutput> LoginPhone([Required] LoginPhoneInput input)
    {
        var verifyCode = _sysCacheService.Get<string>($"{CacheConst.KeyPhoneVerCode}{input.Phone}");
        if (string.IsNullOrWhiteSpace(verifyCode))
            throw Oops.Oh("验证码不存在或已失效，请重新获取！");
        if (verifyCode != input.Code)
            throw Oops.Oh("验证码错误！");

        // 账号是否存在
        var user = await _sysUserRep.AsQueryable().Includes(u => u.SysOrg).ClearFilter().FirstAsync(u => u.Phone.Equals(input.Phone));
        _ = user ?? throw Oops.Oh(ErrorCodeEnum.D0009);

        return await CreateToken(user);
    }

    /// <summary>
    /// 生成Token令牌 🔖
    /// </summary>
    /// <param name="user"></param>
    /// <param name="loginMode"></param>
    /// <returns></returns>
    [NonAction]
    public virtual async Task<LoginOutput> CreateToken(SysUser user, LoginModeEnum loginMode = LoginModeEnum.APP)
    {
        // 单用户登录
        await _sysOnlineUserService.SingleLogin(user.Id, loginMode);

        // 生成Token令牌
        var tokenExpire = await _sysConfigService.GetTokenExpire();
        var accessToken = JWTEncryption.Encrypt(new Dictionary<string, object>
        {
            { AppClaimConst.UserId, user.Id },
            { AppClaimConst.TenantId, user.TenantId },
            { AppClaimConst.Account, user.Account },
            { AppClaimConst.RealName, user.RealName },
            { AppClaimConst.AccountType, user.AccountType },
            { AppClaimConst.OrgId, user.OrgId },
            { AppClaimConst.OrgName, user.SysOrg?.Name },
            { AppClaimConst.OrgType, user.SysOrg?.Type },
            { AppClaimConst.OrgLevel, user.SysOrg?.Level },
            { ClaimConst.LoginMode, loginMode },
            { ClaimConst.TokenVersion, user.TokenVersion },
        }, tokenExpire);

        // 生成刷新Token令牌
        var refreshTokenExpire = await _sysConfigService.GetRefreshTokenExpire();
        var refreshToken = JWTEncryption.GenerateRefreshToken(accessToken, refreshTokenExpire);

        // 设置响应报文头
        _httpContextAccessor.HttpContext.SetTokensOfResponseHeaders(accessToken, refreshToken);

        // 缓存用户Token版本
        _sysCacheService.Set($"{CacheConst.KeyUserToken}{user.Id}", $"{user.TokenVersion}");

        return new LoginOutput
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    /// <summary>
    /// 获取登录账号 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取登录账号")]
    public virtual async Task<LoginUserOutput> GetUserInfo()
    {
        var user = await _sysUserRep.GetByIdAsync(_appUserManager.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D1011).StatusCode(401);
        // 机构
        var org = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysOrg>>().GetByIdAsync(user.OrgId);
        // 职位
        var pos = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysPos>>().GetByIdAsync(user.PosId);
        // 角色集合
        var roles = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysUserRole>>().AsQueryable()
            .LeftJoin<SysRole>((u, a) => u.RoleId == a.Id)
            .Where(u => u.UserId == user.Id)
            .Select((u, a) => new RoleDto { Id = a.Id, Name = a.Name, Code = a.Code }).ToListAsync();
        // 接口集合
        var apis = (await _sysRoleService.GetUserApiList())[0];

        return new LoginUserOutput
        {
            Id = user.Id,
            Account = user.Account,
            RealName = user.RealName,
            Phone = user.Phone,
            IdCardNum = user.IdCardNum,
            Email = user.Email,
            AccountType = user.AccountType,
            Avatar = user.Avatar,
            Address = user.Address,
            Signature = user.Signature,
            OrgId = user.OrgId,
            OrgName = org?.Name,
            OrgType = org?.Type,
            PosName = pos?.Name,
            Apis = apis,
            Roles = roles
        };
    }

    /// <summary>
    /// 获取刷新Token 🔖
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    [DisplayName("获取刷新Token")]
    public virtual string GetRefreshToken([FromQuery] string accessToken)
    {
        var refreshTokenExpire = _sysConfigService.GetRefreshTokenExpire().GetAwaiter().GetResult();
        return JWTEncryption.GenerateRefreshToken(accessToken, refreshTokenExpire);
    }

    /// <summary>
    /// 退出系统 🔖
    /// </summary>
    [DisplayName("退出系统")]
    public void Logout()
    {
        if (string.IsNullOrWhiteSpace(_appUserManager.Account))
            throw Oops.Oh(ErrorCodeEnum.D1011);

        _httpContextAccessor.HttpContext.SignoutToSwagger();
    }

    /// <summary>
    /// 获取验证码 🔖
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [SuppressMonitor]
    [DisplayName("获取验证码")]
    public dynamic GetCaptcha()
    {
        var codeId = YitIdHelper.NextId().ToString();
        var captcha = _captcha.Generate(codeId);
        return new { Id = codeId, Img = captcha.Base64 };
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("修改用户密码")]
    public async Task<int> ChangePwd(ChangePwdInput input)
    {
        // 国密SM2解密（前端密码传输SM2加密后的）
        input.PasswordOld = CryptogramHelper.SM2Decrypt(input.PasswordOld);
        input.PasswordNew = CryptogramHelper.SM2Decrypt(input.PasswordNew);

        var user = await _sysUserRep.GetByIdAsync(_appUserManager.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        if (CryptogramHelper.CryptoType == CryptogramEnum.MD5.ToString())
        {
            if (user.Password != MD5Encryption.Encrypt(input.PasswordOld))
                throw Oops.Oh(ErrorCodeEnum.D1004);
        }
        else
        {
            if (CryptogramHelper.Decrypt(user.Password) != input.PasswordOld)
                throw Oops.Oh(ErrorCodeEnum.D1004);
        }

        if (input.PasswordOld == input.PasswordNew)
            throw Oops.Oh(ErrorCodeEnum.D1028);

        // 验证密码强度
        if (await _sysConfigService.GetConfigValueByCode<bool>(ConfigConst.SysPasswordStrength))
        {
            var sysConfig = await _sysConfigService.GetConfig(ConfigConst.SysPasswordStrengthExpression);
            user.Password = input.PasswordNew.TryValidate(sysConfig.Value)
                ? CryptogramHelper.Encrypt(input.PasswordNew)
                : throw Oops.Oh(sysConfig.Remark);
        }
        else
        {
            user.Password = CryptogramHelper.Encrypt(input.PasswordNew);
        }

        return await _sysUserRep.AsUpdateable(user).UpdateColumns(u => u.Password).ExecuteCommandAsync();
    }
}