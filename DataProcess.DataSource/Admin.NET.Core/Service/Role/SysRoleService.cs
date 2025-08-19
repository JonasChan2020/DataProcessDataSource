// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统角色服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 480, Description = "系统角色")]
public class SysRoleService : IDynamicApiController, ITransient
{
    private readonly UserManager _userManager;
    private readonly SqlSugarRepository<SysRole> _sysRoleRep;
    private readonly SysRoleMenuService _sysRoleMenuService;
    private readonly SysRoleOrgService _sysRoleOrgService;
    private readonly SysRoleTableService _sysRoleTableService;
    private readonly SysRoleApiService _sysRoleApiService;
    private readonly SysOrgService _sysOrgService;
    private readonly SysUserRoleService _sysUserRoleService;
    private readonly SysCacheService _sysCacheService;
    private readonly SysCommonService _sysCommonService;

    public SysRoleService(UserManager userManager,
        SqlSugarRepository<SysRole> sysRoleRep,
        SysRoleMenuService sysRoleMenuService,
        SysRoleOrgService sysRoleOrgService,
        SysRoleTableService sysRoleTableService,
        SysRoleApiService sysRoleApiService,
        SysOrgService sysOrgService,
        SysUserRoleService sysUserRoleService,
        SysCacheService sysCacheService,
        SysCommonService sysCommonService)
    {
        _userManager = userManager;
        _sysRoleRep = sysRoleRep;
        _sysRoleMenuService = sysRoleMenuService;
        _sysRoleOrgService = sysRoleOrgService;
        _sysRoleTableService = sysRoleTableService;
        _sysRoleApiService = sysRoleApiService;
        _sysOrgService = sysOrgService;
        _sysUserRoleService = sysUserRoleService;
        _sysCacheService = sysCacheService;
        _sysCommonService = sysCommonService;
    }

    /// <summary>
    /// 获取角色分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取角色分页列表")]
    public async Task<SqlSugarPagedList<PageRoleOutput>> Page(PageRoleInput input)
    {
        // 当前用户已拥有的角色集合
        var roleIdList = _userManager.SuperAdmin ? new List<long>() : await _sysUserRoleService.GetUserRoleIdList(_userManager.UserId);

        return await _sysRoleRep.AsQueryable()
            .LeftJoin<SysTenant>((u, a) => u.TenantId == a.Id)
            .LeftJoin<SysOrg>((u, a, b) => a.OrgId == b.Id)
            .WhereIF(!_userManager.SuperAdmin, u => u.TenantId == _userManager.TenantId) // 若非超管，则只能操作本租户的角色
            .WhereIF(!_userManager.SuperAdmin && !_userManager.SysAdmin, u => u.CreateUserId == _userManager.UserId || roleIdList.Contains(u.Id)) // 若非超管且非系统管理员，则只能操作自己创建的角色
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), u => u.Code.Contains(input.Code))
            .Select((u, a, b) => new PageRoleOutput
            {
                TenantName = b.Name
            }, true)
            .OrderBy(u => new { u.OrderNo, u.Id })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取角色列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取角色列表")]
    public async Task<List<RoleOutput>> GetList()
    {
        // 当前用户已拥有的角色集合
        var roleIdList = _userManager.SuperAdmin ? [] : await _sysUserRoleService.GetUserRoleIdList(_userManager.UserId);

        return await _sysRoleRep.AsQueryable()
            .WhereIF(!_userManager.SuperAdmin, u => u.TenantId == _userManager.TenantId) // 若非超管，则只能操作本租户的角色
            .WhereIF(!_userManager.SuperAdmin && !_userManager.SysAdmin, u => u.CreateUserId == _userManager.UserId || roleIdList.Contains(u.Id)) // 若非超管且非系统管理员，则只显示自己创建和已拥有的角色
            .Where(u => u.Status != StatusEnum.Disable) // 非禁用的
            .OrderBy(u => new { u.OrderNo, u.Id }).Select(u => new RoleOutput { Disabled = false }, true).ToListAsync();
    }

    /// <summary>
    /// 增加角色 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加角色")]
    public async Task AddRole(AddRoleInput input)
    {
        if (await _sysRoleRep.IsAnyAsync(u => u.Name == input.Name && u.Code == input.Code))
            throw Oops.Oh(ErrorCodeEnum.D1006);

        var roleId = await _sysRoleRep.AsInsertable(input.Adapt<SysRole>()).ExecuteReturnSnowflakeIdAsync();

        // 授权角色基础菜单集合
        var menuIdList = new List<long>
        {
            1300000000101, 1300000000111, 1300000000121, // 工作台
            1310000000161, 1310000000162, 1310000000163, 1310000000164, 1310000000165, // 个人中心
        };
        await _sysRoleMenuService.GrantRoleMenu(new RoleMenuInput() { Id = roleId, MenuIdList = menuIdList });
    }

    /// <summary>
    /// 更新角色 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新角色")]
    public async Task UpdateRole(UpdateRoleInput input)
    {
        if (await _sysRoleRep.IsAnyAsync(u => u.Name == input.Name && u.Code == input.Code && u.Id != input.Id))
            throw Oops.Oh(ErrorCodeEnum.D1006);

        await _sysRoleRep.AsUpdateable(input.Adapt<SysRole>()).IgnoreColumns(true)
            .IgnoreColumns(u => new { u.DataScope }).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除角色 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除角色")]
    public async Task DeleteRole(DeleteRoleInput input)
    {
        // 禁止删除系统内置角色
        var sysRole = await _sysRoleRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        if (sysRole.SysFlag == YesNoEnum.Y) throw Oops.Oh(ErrorCodeEnum.D1019);

        // 若角色有用户则禁止删除
        var userIds = await _sysUserRoleService.GetUserIdList(input.Id);
        if (userIds != null && userIds.Count > 0) throw Oops.Oh(ErrorCodeEnum.D1025);

        await _sysRoleRep.DeleteAsync(sysRole);

        // 级联删除角色机构数据
        await _sysRoleOrgService.DeleteRoleOrgByRoleId(sysRole.Id);

        // 级联删除用户角色数据
        await _sysUserRoleService.DeleteUserRoleByRoleId(sysRole.Id);

        // 级联删除角色菜单数据
        await _sysRoleMenuService.DeleteRoleMenuByRoleId(sysRole.Id);

        // 级联删除角色接口数据
        await _sysRoleApiService.DeleteRoleApiByRoleId(sysRole.Id);

        // 级联删除角色表格数据
        await _sysRoleTableService.DeleteRolTableByRoleId(sysRole.Id);
    }

    /// <summary>
    /// 复制角色 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Copy"), HttpPost]
    [DisplayName("复制角色")]
    public async Task CopyRole(CopyRoleInput input)
    {
        // 查找角色
        var sysRole = await _sysRoleRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);

        // 新增角色
        var newRoleId = YitIdHelper.NextId();
        sysRole.Id = newRoleId;
        sysRole.Code = $"{sysRole.Code} - copy";
        sysRole.Name = $"{sysRole.Name} - copy";
        sysRole.CreateTime = DateTime.Now;
        sysRole.CreateUserId = null;
        sysRole.CreateUserName = null;
        sysRole.UpdateTime = null;
        sysRole.UpdateUserId = null;
        sysRole.UpdateUserName = null;
        await _sysRoleRep.InsertAsync(sysRole);

        // 复制角色数据范围
        await _sysRoleOrgService.CopyRoleOrgByRoleId(input.Id, newRoleId);

        // 复制角色菜单数据
        await _sysRoleMenuService.CopyRoleMenuByRoleId(input.Id, newRoleId);

        // 复制角色接口数据
        await _sysRoleApiService.CopyRoleApiByRoleId(input.Id, newRoleId);

        // 复制角色表格数据
        await _sysRoleTableService.CopyRolTableByRoleId(input.Id, newRoleId);
    }

    /// <summary>
    /// 授权角色菜单 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("授权角色菜单")]
    public async Task GrantMenu(RoleMenuInput input)
    {
        if (input.MenuIdList == null || input.MenuIdList.Count < 1) return;

        //// 将父节点为0的菜单排除，防止前端全选异常
        //var pMenuIds = await _sysRoleRep.ChangeRepository<SqlSugarRepository<SysMenu>>().AsQueryable().Where(u => input.MenuIdList.Contains(u.Id) && u.Pid == 0).ToListAsync(u => u.Id);
        //var menuIds = input.MenuIdList.Except(pMenuIds); // 差集
        //await _sysRoleMenuService.GrantRoleMenu(new RoleMenuInput()
        //{
        //    Id = input.Id,
        //    MenuIdList = menuIds.ToList()
        //});

        await _sysRoleMenuService.GrantRoleMenu(input);

        await ClearUserApiCache(input.Id);
    }

    /// <summary>
    /// 授权角色表格 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("授权角色表格")]
    public async Task GrantRoleTable(RoleTableInput input)
    {
        await _sysRoleTableService.GrantRoleTable(input);
    }

    /// <summary>
    /// 授权角色数据范围 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("授权角色数据范围")]
    public async Task GrantDataScope(RoleOrgInput input)
    {
        // 删除与该角色相关的用户机构缓存
        var userIdList = await _sysUserRoleService.GetUserIdList(input.Id);
        foreach (var userId in userIdList)
        {
            SqlSugarFilter.DeleteUserOrgCache(userId, _sysRoleRep.Context.CurrentConnectionConfig.ConfigId.ToString());
        }

        var role = await _sysRoleRep.GetFirstAsync(u => u.Id == input.Id);
        var dataScope = input.DataScope;
        if (!_userManager.SuperAdmin)
        {
            switch (dataScope)
            {
                //// 非超级管理员没有全部数据范围权限
                //case (int)DataScopeEnum.All: throw Oops.Oh(ErrorCodeEnum.D1016);
                // 若数据范围自定义，则判断授权数据范围是否有权限
                case (int)DataScopeEnum.Define:
                    {
                        var grantOrgIdList = input.OrgIdList;
                        if (grantOrgIdList.Count > 0)
                        {
                            var orgIdList = await _sysOrgService.GetUserOrgIdList();
                            if (orgIdList.Count < 1) throw Oops.Oh(ErrorCodeEnum.D1016);
                            if (!grantOrgIdList.All(u => orgIdList.Any(c => c == u))) throw Oops.Oh(ErrorCodeEnum.D1016);
                        }

                        break;
                    }

                default:
                    break;
            }
        }

        role.DataScope = (DataScopeEnum)dataScope;
        await _sysRoleRep.AsUpdateable(role).UpdateColumns(u => new { u.DataScope }).ExecuteCommandAsync();
        await _sysRoleOrgService.GrantRoleOrg(input);
    }

    /// <summary>
    /// 授权角色接口 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("授权角色接口")]
    public async Task GrantApi(RoleApiInput input)
    {
        await ClearUserApiCache(input.Id);
        await _sysRoleApiService.GrantRoleApi(input);
    }

    /// <summary>
    /// 授权角色用户 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("授权角色用户")]
    public async Task GrantUser(RoleUserInput input)
    {
        await _sysUserRoleService.GrantRoleUser(input);
    }

    /// <summary>
    /// 设置角色状态 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("设置角色状态")]
    public async Task<int> SetStatus(RoleInput input)
    {
        if (!Enum.IsDefined(typeof(StatusEnum), input.Status)) throw Oops.Oh(ErrorCodeEnum.D3005);

        return await _sysRoleRep.AsUpdateable()
            .SetColumns(u => u.Status == input.Status)
            .Where(u => u.Id == input.Id)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取所有表格字段 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取所有表格字段")]
    public List<RoleTableOutput> GetAllTableColumnList()
    {
        return _sysRoleTableService.HandleTableColumn();
    }

    /// <summary>
    /// 获取角色表格字段集合 🔖
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    [DisplayName("获取角色表格字段集合")]
    public async Task<List<string>> GetRoleTable(long roleId)
    {
        return await _sysRoleTableService.GetRoleTable(roleId);
    }

    /// <summary>
    /// 获取当前用户表格字段集合 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取当前用户表格字段集合")]
    public async Task<List<string>> GetUserRoleTableList()
    {
        return await _sysRoleTableService.GetUserRoleTableList(_userManager, _sysUserRoleService);
    }

    /// <summary>
    /// 根据角色Id获取菜单Id集合 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据角色Id获取菜单Id集合")]
    public async Task<List<long>> GetOwnMenuList([FromQuery] RoleInput input)
    {
        return await _sysRoleMenuService.GetRoleMenuIdList(new List<long> { input.Id });
    }

    /// <summary>
    /// 根据角色Id获取机构Id集合 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据角色Id获取机构Id集合")]
    public async Task<List<long>> GetOwnOrgList([FromQuery] RoleInput input)
    {
        return await _sysRoleOrgService.GetRoleOrgIdList(new List<long> { input.Id });
    }

    /// <summary>
    /// 获取角色接口黑名单集合 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取角色接口黑名单集合")]
    public async Task<List<string>> GetRoleApiList([FromQuery] RoleInput input)
    {
        return await _sysRoleApiService.GetRoleApiList(new List<long> { input.Id });

        //var roleButtons = await GetRoleButtonList(new List<long> { input.Id });
        //return roleApis.Union(roleButtons).ToList();
    }

    /// <summary>
    /// 获取用户接口集合 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取用户接口集合")]
    public async Task<List<List<string>>> GetUserApiList()
    {
        var userId = _userManager.UserId;
        var apiList = _sysCacheService.Get<List<List<string>>>(CacheConst.KeyUserApi + userId);
        if (apiList != null) return apiList;

        apiList = [[], []];
        // 所有按钮权限集合
        var allButtonList = await GetButtonList();
        // 超管账号获取所有接口
        if (_userManager.SuperAdmin)
        {
            var allApiList = _sysCommonService.GetApiList();
            foreach (var apiOutput in allApiList)
            {
                foreach (var controller in apiOutput.Children)
                    apiList[0].AddRange(controller.Children.Select(u => u.Route));
            }

            // 接口没有对应的按钮权限集合
            var diffButtonList = allButtonList.Except(apiList[0]).ToList(); // 差集
            apiList[0].AddRange(diffButtonList);
        }
        else
        {
            // 当前账号所有角色集合
            var roleIdList = await _sysUserRoleService.GetUserRoleIdList(_userManager.UserId);
            // 已有按钮权限集合
            var menuIdList = await _sysRoleMenuService.GetRoleMenuIdList(roleIdList);
            apiList[0] = await GetButtonList(menuIdList, false);

            // 未有按钮权限集合（放到接口黑名单里面）
            apiList[1] = allButtonList.Except(apiList[0]).ToList(); // 差集
            // 接口黑名单集合
            var roleApiList = await _sysRoleApiService.GetRoleApiList(roleIdList);
            apiList[1].AddRange(roleApiList);
        }

        // 排序接口名称
        apiList[0].Sort();
        apiList[1].Sort();
        _sysCacheService.Set(CacheConst.KeyUserApi + userId, apiList, TimeSpan.FromDays(7)); // 缓存7天
        return apiList;
    }

    ///// <summary>
    ///// 获取用户按钮权限集合
    ///// </summary>
    ///// <returns></returns>
    //[NonAction]
    //public async Task<List<string>> GetUserButtonList()
    //{
    //    var menuIdList = new List<long>();
    //    if (!_userManager.SuperAdmin)
    //    {
    //        var roleIdList = await _sysUserRoleService.GetUserRoleIdList(_userManager.UserId);
    //        menuIdList = await _sysRoleMenuService.GetRoleMenuIdList(roleIdList);
    //    }
    //    return await GetButtonList(menuIdList);
    //}

    ///// <summary>
    ///// 获取角色按钮权限集合
    ///// </summary>
    ///// <param name="roleIds"></param>
    ///// <returns></returns>
    //[NonAction]
    //public async Task<List<string>> GetRoleButtonList(List<long> roleIds)
    //{
    //    var menuIdList = await _sysRoleMenuService.GetRoleMenuIdList(roleIds);
    //    return await GetButtonList(menuIdList);
    //}

    /// <summary>
    /// 根据菜单Id集合获取按钮集合
    /// </summary>
    /// <param name="menuIds"></param>
    /// <param name="isAll"></param>
    /// <returns></returns>
    private async Task<List<string>> GetButtonList(List<long> menuIds = null, bool isAll = true)
    {
        return await _sysRoleRep.ChangeRepository<SqlSugarRepository<SysMenu>>().AsQueryable()
            .WhereIF(menuIds != null && menuIds.Count > 0, u => menuIds.Contains(u.Id))
            .WhereIF(!isAll, u => u.Status == StatusEnum.Enable)
            .Where(u => u.Type == MenuTypeEnum.Btn)
            .Select(u => u.Permission).ToListAsync();
    }

    /// <summary>
    /// 删除与该角色相关的用户接口缓存
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task ClearUserApiCache(long roleId)
    {
        var userIdList = await _sysUserRoleService.GetUserIdList(roleId);
        foreach (var userId in userIdList)
        {
            _sysCacheService.Remove(CacheConst.KeyUserApi + userId);
        }
    }
}