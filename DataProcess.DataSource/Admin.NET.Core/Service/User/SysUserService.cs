// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统用户服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 490, Description = "系统用户")]
public class SysUserService : IDynamicApiController, ITransient
{
    private readonly UserManager _userManager;
    private readonly SysOrgService _sysOrgService;
    private readonly SysUserExtOrgService _sysUserExtOrgService;
    private readonly SysRoleService _sysRoleService;
    private readonly SysUserRoleService _sysUserRoleService;
    private readonly SysConfigService _sysConfigService;
    private readonly SysOnlineUserService _sysOnlineUserService;
    private readonly SysUserMenuService _sysUserMenuService;
    private readonly SysCacheService _sysCacheService;
    private readonly SysUserLdapService _sysUserLdapService;
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly IEventPublisher _eventPublisher;

    public SysUserService(UserManager userManager,
        SysOrgService sysOrgService,
        SysUserExtOrgService sysUserExtOrgService,
        SysRoleService sysRoleService,
        SysUserRoleService sysUserRoleService,
        SysConfigService sysConfigService,
        SysOnlineUserService sysOnlineUserService,
        SysUserMenuService sysUserMenuService,
        SysCacheService sysCacheService,
        SysUserLdapService sysUserLdapService,
        SqlSugarRepository<SysUser> sysUserRep,
        IEventPublisher eventPublisher)
    {
        _userManager = userManager;
        _sysOrgService = sysOrgService;
        _sysUserExtOrgService = sysUserExtOrgService;
        _sysRoleService = sysRoleService;
        _sysUserRoleService = sysUserRoleService;
        _sysConfigService = sysConfigService;
        _sysOnlineUserService = sysOnlineUserService;
        _sysUserMenuService = sysUserMenuService;
        _sysCacheService = sysCacheService;
        _sysUserLdapService = sysUserLdapService;
        _sysUserRep = sysUserRep;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// 获取用户分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取用户分页列表")]
    public virtual async Task<SqlSugarPagedList<UserOutput>> Page(PageUserInput input)
    {
        // 获取用户拥有的机构集合
        var userOrgIdList = await _sysOrgService.GetUserOrgIdList();
        List<long> orgList = null;
        List<long> extOrgUserIdList = null;
        if (input.OrgId > 0) // 指定机构查询时
        {
            orgList = await _sysOrgService.GetChildIdListWithSelfById(input.OrgId);
            var extOrgList = await _sysUserExtOrgService.GetUserExtOrgListForOrg(input.OrgId);
            extOrgUserIdList = extOrgList.Select(u => u.UserId).ToList();
            orgList = _userManager.SuperAdmin ? orgList : orgList.Where(u => userOrgIdList.Contains(u)).ToList();
        }
        else // 各管理员只能看到自己机构下的用户列表
        {
            orgList = _userManager.SuperAdmin ? null : userOrgIdList;
        }

        return await _sysUserRep.AsQueryable()
            .LeftJoin<SysOrg>((u, a) => u.OrgId == a.Id)
            .LeftJoin<SysPos>((u, a, b) => u.PosId == b.Id)
            .Where(u => u.AccountType != AccountTypeEnum.SuperAdmin)
            .WhereIF(orgList != null || extOrgUserIdList != null, u => orgList.Contains(u.OrgId) || extOrgUserIdList.Contains(u.Id))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Account), u => u.Account.Contains(input.Account))
            .WhereIF(!string.IsNullOrWhiteSpace(input.RealName), u => u.RealName.Contains(input.RealName))
            .WhereIF(!string.IsNullOrWhiteSpace(input.PosName), (u, a, b) => b.Name.Contains(input.PosName))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Phone), u => u.Phone.Contains(input.Phone))
            .OrderBy(u => new { u.OrderNo, u.Id })
            .Select((u, a, b) => new UserOutput
            {
                OrgName = a.Name,
                PosName = b.Name,
                RoleName = SqlFunc.Subqueryable<SysUserRole>().LeftJoin<SysRole>((m, n) => m.RoleId == n.Id).Where(m => m.UserId == u.Id).SelectStringJoin((m, n) => n.Name, ","),
                DomainAccount = SqlFunc.Subqueryable<SysUserLdap>().Where(m => m.UserId == u.Id).Select(m => m.Account)
            }, true)
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加用户 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加用户")]
    public virtual async Task<long> AddUser(AddUserInput input)
    {
        // 是否有权操作此账号
        if (!_userManager.SuperAdmin && input.AccountType is AccountTypeEnum.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D1033);

        var query = _sysUserRep.AsQueryable().ClearFilter();
        if (await query.AnyAsync(u => u.Account == input.Account)) throw Oops.Oh(ErrorCodeEnum.D1003);
        if (!string.IsNullOrWhiteSpace(input.Phone) && await query.AnyAsync(u => u.Phone == input.Phone)) throw Oops.Oh(ErrorCodeEnum.D1032);

        // 若没有设置密码则取默认密码
        var password = !string.IsNullOrWhiteSpace(input.Password) ? input.Password : await _sysConfigService.GetConfigValueByCode<string>(ConfigConst.SysPassword);
        var user = input.Adapt<SysUser>();
        user.Password = CryptogramHelper.Encrypt(password);
        var newUser = await _sysUserRep.AsInsertable(user).ExecuteReturnEntityAsync();

        input.Id = newUser.Id;
        await UpdateRoleAndExtOrg(input);

        // 增加域账号
        if (!string.IsNullOrWhiteSpace(input.DomainAccount))
            await _sysUserLdapService.AddUserLdap(newUser.TenantId!.Value, newUser.Id, newUser.Account, input.DomainAccount);

        // 发布新增用户事件
        await _eventPublisher.PublishAsync(UserEventTypeEnum.Add, input);

        return newUser.Id;
    }

    /// <summary>
    /// 更新用户 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新用户")]
    public virtual async Task UpdateUser(UpdateUserInput input)
    {
        var query = _sysUserRep.AsQueryable().ClearFilter().Where(u => u.Id != input.Id);
        if (await query.AnyAsync(u => u.Account == input.Account)) throw Oops.Oh(ErrorCodeEnum.D1003);
        if (!string.IsNullOrWhiteSpace(input.Phone) && await query.AnyAsync(u => u.Phone == input.Phone)) throw Oops.Oh(ErrorCodeEnum.D1032);

        // 若账号的角色和组织架构发生变化,则强制下线账号进行权限更新
        var user = await _sysUserRep.GetByIdAsync(input.Id);
        var roleIds = await _sysUserRoleService.GetUserRoleIdList(input.Id);
        if (input.OrgId != user.OrgId || !input.RoleIdList.OrderBy(u => u).SequenceEqual(roleIds.OrderBy(u => u)))
        {
            // 强制下线账号和失效Token
            await OfflineAndExpireToken(user);
        }

        // 更新用户
        await _sysUserRep.AsUpdateable(input.Adapt<SysUser>()).IgnoreColumns(true).IgnoreColumns(u => new { u.Password, u.Status, u.TenantId }).ExecuteCommandAsync();

        // 更新用户附属机构
        await UpdateRoleAndExtOrg(input);

        // 删除用户机构缓存
        SqlSugarFilter.DeleteUserOrgCache(input.Id, _sysUserRep.Context.CurrentConnectionConfig.ConfigId.ToString());

        // 更新域账号
        await _sysUserLdapService.AddUserLdap(user.TenantId!.Value, user.Id, user.Account, input.DomainAccount);

        // 发布更新用户事件
        await _eventPublisher.PublishAsync(UserEventTypeEnum.Update, input);
    }

    /// <summary>
    /// 更新角色和扩展机构
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task UpdateRoleAndExtOrg(AddUserInput input)
    {
        await GrantRole(new UserRoleInput { UserId = input.Id, RoleIdList = input.RoleIdList });

        await _sysUserExtOrgService.UpdateUserExtOrg(input.Id, input.ExtOrgIdList);
    }

    /// <summary>
    /// 删除用户 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除用户")]
    public virtual async Task DeleteUser(DeleteUserInput input)
    {
        var user = await _sysUserRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        user.ValidateIsSuperAdminAccountType();
        user.ValidateIsUserId(_userManager.UserId);

        // 若账号为租户默认账号则禁止删除
        var isTenantUser = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysTenant>>().IsAnyAsync(u => u.UserId == input.Id);
        if (isTenantUser) throw Oops.Oh(ErrorCodeEnum.D1029);

        // 若账号为开放接口绑定账号则禁止删除
        var isOpenAccessUser = await _sysUserRep.ChangeRepository<SqlSugarRepository<SysOpenAccess>>().IsAnyAsync(u => u.BindUserId == input.Id);
        if (isOpenAccessUser) throw Oops.Oh(ErrorCodeEnum.D1030);

        // 强制下线账号和失效Token
        await OfflineAndExpireToken(user);

        // 删除用户
        await _sysUserRep.DeleteAsync(user);

        // 删除用户角色
        await _sysUserRoleService.DeleteUserRoleByUserId(input.Id);

        // 删除用户扩展机构
        await _sysUserExtOrgService.DeleteUserExtOrgByUserId(input.Id);

        // 删除域账号
        await _sysUserLdapService.DeleteUserLdapByUserId(input.Id);

        // 删除用户收藏菜单
        await _sysUserMenuService.DeleteUserMenuList(input.Id);

        // 发布删除用户事件
        await _eventPublisher.PublishAsync(UserEventTypeEnum.Delete, input);
    }

    /// <summary>
    /// 查看用户基本信息 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("查看用户基本信息")]
    public virtual async Task<SysUser> GetBaseInfo()
    {
        return await _sysUserRep.GetByIdAsync(_userManager.UserId);
    }

    /// <summary>
    /// 更新用户基本信息 🔖
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "UpdateBaseInfo"), HttpPost]
    [DisplayName("更新用户基本信息")]
    public virtual async Task<int> UpdateBaseInfo(SysUser user)
    {
        return await _sysUserRep.AsUpdateable(user)
            .IgnoreColumns(u => new { u.CreateTime, u.Account, u.Password, u.AccountType, u.OrgId, u.PosId }).ExecuteCommandAsync();
    }

    /// <summary>
    /// 设置用户状态 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("设置用户状态")]
    public virtual async Task<int> SetStatus(BaseStatusInput input)
    {
        if (_userManager.UserId == input.Id)
            throw Oops.Oh(ErrorCodeEnum.D1026);

        var user = await _sysUserRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        user.ValidateIsSuperAdminAccountType(ErrorCodeEnum.D1015);

        if (!Enum.IsDefined(typeof(StatusEnum), input.Status))
            throw Oops.Oh(ErrorCodeEnum.D3005);

        if (input.Status != StatusEnum.Enable)
        {
            // 强制下线账号和失效Token
            await OfflineAndExpireToken(user);
        }

        user.Status = input.Status;
        var rows = await _sysUserRep.AsUpdateable(user).UpdateColumns(u => new { u.Status }).ExecuteCommandAsync();

        // 发布设置用户状态事件
        await _eventPublisher.PublishAsync(UserEventTypeEnum.SetStatus, input);

        return rows;
    }

    /// <summary>
    /// 授权用户角色 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("授权用户角色")]
    public async Task GrantRole(UserRoleInput input)
    {
        var user = await _sysUserRep.GetByIdAsync(input.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D0009);

        // 若角色发生改变则进行更新
        var roles = await _sysUserRoleService.GetUserRoleIdList(input.UserId);
        if (!roles.SequenceEqual(input.RoleIdList))
        {
            // 更新用户角色
            await _sysUserRoleService.GrantUserRole(input);
            // 强制下线账号和失效Token
            await OfflineAndExpireToken(user);
            // 发布更新用户角色事件
            await _eventPublisher.PublishAsync(UserEventTypeEnum.UpdateRole, input);
        }
    }

    /// <summary>
    /// 修改用户密码 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("修改用户密码")]
    public virtual async Task<int> ChangePwd(ChangePwdInput input)
    {
        // 国密SM2解密（前端密码传输SM2加密后的）
        input.PasswordOld = CryptogramHelper.SM2Decrypt(input.PasswordOld);
        input.PasswordNew = CryptogramHelper.SM2Decrypt(input.PasswordNew);

        var user = await _sysUserRep.GetByIdAsync(_userManager.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
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

        // 验证历史密码记录
        var sysUserPasswordRecord = _sysUserRep.ChangeRepository<SqlSugarRepository<SysUserPasswordRecord>>();
        if (await _sysConfigService.GetConfigValueByCode<bool>(ConfigConst.SysPasswordRecord))
        {
            var md5 = MD5Encryption.Encrypt(input.PasswordNew);
            if (await sysUserPasswordRecord.IsAnyAsync(u => u.UserId == user.Id && u.Password == md5))
                throw Oops.Oh(ErrorCodeEnum.D1104);
        }
        // 增加密码历史记录（统一MD5存储）
        await sysUserPasswordRecord.InsertAsync(new SysUserPasswordRecord { UserId = user.Id, Password = MD5Encryption.Encrypt(input.PasswordNew) });

        // 更新密码和最新修改时间
        user.LastChangePasswordTime = DateTime.Now;
        user.TokenVersion++;
        var rows = await _sysUserRep.AsUpdateable(user).UpdateColumns(u => new { u.Password, u.LastChangePasswordTime, u.TokenVersion }).ExecuteCommandAsync();

        // 强制下线账号和失效Token
        await OfflineAndExpireToken(user);

        // 发布修改用户密码事件
        await _eventPublisher.PublishAsync(UserEventTypeEnum.ChangePwd, input);

        return rows;
    }

    /// <summary>
    /// 重置用户密码 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("重置用户密码")]
    public virtual async Task<string> ResetPwd(ResetPwdUserInput input)
    {
        var user = await _sysUserRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D0009);
        var password = await _sysConfigService.GetConfigValueByCode<string>(ConfigConst.SysPassword);
        if (!string.IsNullOrEmpty(input.NewPassword))
            password = input.NewPassword;
        user.Password = CryptogramHelper.Encrypt(password);
        user.LastChangePasswordTime = null;
        user.TokenVersion++;
        await _sysUserRep.AsUpdateable(user).UpdateColumns(u => new { u.Password, u.LastChangePasswordTime, u.TokenVersion }).ExecuteCommandAsync();

        // 清空密码错误次数缓存
        _sysCacheService.Remove($"{CacheConst.KeyPasswordErrorTimes}{user.Account}");

        // 强制下线账号和失效Token
        await OfflineAndExpireToken(user);

        // 发布重置用户密码事件
        await _eventPublisher.PublishAsync(UserEventTypeEnum.ResetPwd, input);

        return password;
    }

    /// <summary>
    /// 验证密码有效期 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("验证密码有效期")]
    public async Task<bool> VerifyPwdExpirationTime()
    {
        var sysConfig = await _sysConfigService.GetConfig(ConfigConst.SysPasswordExpirationTime);
        if (int.TryParse(sysConfig.Value, out int expirationTime) && expirationTime > 0)
        {
            var user = await _sysUserRep.GetByIdAsync(_userManager.UserId);
            if (user.LastChangePasswordTime == null)
                return false;
            if ((DateTime.Now - user.LastChangePasswordTime.Value).Days > expirationTime)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 解除登录锁定 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("解除登录锁定")]
    public virtual async Task UnlockLogin(BaseIdInput input)
    {
        var user = await _sysUserRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D0009);

        // 清空密码错误次数
        var keyPasswordErrorTimes = $"{CacheConst.KeyPasswordErrorTimes}{user.Account}";
        _sysCacheService.Remove(keyPasswordErrorTimes);

        // 发布解除登录锁定事件
        await _eventPublisher.PublishAsync(UserEventTypeEnum.UnlockLogin, input);
    }

    /// <summary>
    /// 获取用户拥有角色集合 🔖
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [DisplayName("获取用户拥有角色集合")]
    public async Task<GrantRoleOutput> GetOwnRoleList(long userId)
    {
        // 获取当前用户已分配角色
        var grantedRoles = (await _sysUserRoleService.GetUserRoleInfoList(userId));
        // 获取当前用户可用的角色
        var availableRoles = await _sysRoleService.GetList();
        // 改变用户分配的角色可分配状态
        grantedRoles.ForEach(u => u.Disabled = !availableRoles.Any(e => e.Id == u.Id));
        // 排除已分配的角色
        availableRoles = availableRoles.ExceptBy(grantedRoles.Select(u => u.Id), u => u.Id).ToList();
        return new GrantRoleOutput { GrantedRoles = grantedRoles, AvailableRoles = availableRoles };
    }

    /// <summary>
    /// 获取用户扩展机构集合 🔖
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [DisplayName("获取用户扩展机构集合")]
    public async Task<List<SysUserExtOrg>> GetOwnExtOrgList(long userId)
    {
        return await _sysUserExtOrgService.GetUserExtOrgList(userId);
    }

    /// <summary>
    /// 强制下线账号和失效Token
    /// </summary>
    /// <param name="user"></param>
    private async Task OfflineAndExpireToken(SysUser user)
    {
        // 当角色、机构、密码、重置、删除、状态改变时，删除Token版本缓存
        _sysCacheService.Remove($"{CacheConst.KeyUserToken}{user.Id}");

        // 强制下线账号
        await _sysOnlineUserService.ForceOfflineByUserId(user.Id);
    }
}