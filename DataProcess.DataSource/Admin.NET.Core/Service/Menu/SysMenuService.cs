// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统菜单服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 450, Description = "系统菜单")]
public class SysMenuService : IDynamicApiController, ITransient
{
    private readonly UserManager _userManager;
    private readonly SqlSugarRepository<SysMenu> _sysMenuRep;
    private readonly SysRoleMenuService _sysRoleMenuService;
    private readonly SysUserRoleService _sysUserRoleService;
    private readonly SysUserMenuService _sysUserMenuService;

    public SysMenuService(UserManager userManager,
        SqlSugarRepository<SysMenu> sysMenuRep,
        SysRoleMenuService sysRoleMenuService,
        SysUserRoleService sysUserRoleService,
        SysUserMenuService sysUserMenuService)
    {
        _userManager = userManager;
        _sysMenuRep = sysMenuRep;
        _sysRoleMenuService = sysRoleMenuService;
        _sysUserRoleService = sysUserRoleService;
        _sysUserMenuService = sysUserMenuService;
    }

    /// <summary>
    /// 获取登录菜单树 🔖
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("获取登录菜单树")]
    public async Task<List<MenuOutput>> GetLoginMenuTree()
    {
        // var test = App.HttpContext.Request.Headers["Accept-Language"];
        // Console.WriteLine($"接收到的accept-language: {test}");
        // Console.WriteLine($"翻译: {L.Text["差异日志"]}");
        if (_userManager.SuperAdmin)
        {
            var menuList = await _sysMenuRep.AsQueryable()
                .Where(u => u.Type != MenuTypeEnum.Btn && u.Status == StatusEnum.Enable)
                .OrderBy(u => new { u.OrderNo, u.Id })
                .Distinct().ToTreeAsync(u => u.Children, u => u.Pid, 0);
            return menuList.Adapt<List<MenuOutput>>();
        }
        else
        {
            var menuIdList = await GetMenuIdList();
            var menuTree = await _sysMenuRep.AsQueryable()
                .Where(u => u.Status == StatusEnum.Enable)
                .OrderBy(u => new { u.OrderNo, u.Id })
                .Distinct().ToTreeAsync(u => u.Children, u => u.Pid, 0, menuIdList.Select(d => (object)d).ToArray());
            DeleteBtnFromMenuTree(menuTree);
            return menuTree.Adapt<List<MenuOutput>>();
        }
    }

    /// <summary>
    /// 删除登录菜单树里面的按钮
    /// </summary>
    private static void DeleteBtnFromMenuTree(List<SysMenu> menuList)
    {
        if (menuList == null) return;
        for (var i = menuList.Count - 1; i >= 0; i--)
        {
            var menu = menuList[i];
            if (menu.Type == MenuTypeEnum.Btn)
                menuList.Remove(menu);
            else if (menu.Children.Count > 0)
                DeleteBtnFromMenuTree(menu.Children);
        }
    }

    /// <summary>
    /// 获取菜单列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取菜单列表")]
    public async Task<List<SysMenu>> GetList([FromQuery] MenuInput input)
    {
        var menuIdList = _userManager.SuperAdmin ? [] : await GetMenuIdList();

        // 有筛选条件时返回list列表（防止构造不出树）
        if (!string.IsNullOrWhiteSpace(input.Title) || input.Type is > 0)
        {
            return await _sysMenuRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(input.Title), u => u.Title.Contains(input.Title))
                .WhereIF(input.Type is > 0, u => u.Type == input.Type)
                .WhereIF(menuIdList.Count > 1, u => menuIdList.Contains(u.Id))
                .OrderBy(u => new { u.OrderNo, u.Id })
                .Distinct().ToListAsync();
        }

        return _userManager.SuperAdmin ?
            await _sysMenuRep.AsQueryable().OrderBy(u => new { u.OrderNo, u.Id })
                .Distinct().Distinct().ToTreeAsync(u => u.Children, u => u.Pid, 0) :
            await _sysMenuRep.AsQueryable().OrderBy(u => new { u.OrderNo, u.Id })
                .Distinct().ToTreeAsync(u => u.Children, u => u.Pid, 0, menuIdList.Select(d => (object)d).ToArray());
    }

    /// <summary>
    /// 增加菜单 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加菜单")]
    public async Task AddMenu(AddMenuInput input)
    {
        var isExist = input.Type != MenuTypeEnum.Btn
            ? await _sysMenuRep.IsAnyAsync(u => u.Title == input.Title && u.Pid == input.Pid)
            : await _sysMenuRep.IsAnyAsync(u => u.Pid == input.Pid && u.Permission == input.Permission);
        if (isExist)
            throw Oops.Oh(ErrorCodeEnum.D4000);

        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            if (await _sysMenuRep.IsAnyAsync(u => u.Name == input.Name))
                throw Oops.Oh(ErrorCodeEnum.D4009);
        }

        if (input.Pid != 0)
        {
            if (await _sysMenuRep.IsAnyAsync(u => u.Id == input.Pid && u.Type == MenuTypeEnum.Btn))
                throw Oops.Oh(ErrorCodeEnum.D4010);
        }

        // 校验菜单参数
        var sysMenu = input.Adapt<SysMenu>();
        CheckMenuParam(sysMenu);

        await _sysMenuRep.InsertAsync(sysMenu);
    }

    /// <summary>
    /// 更新菜单 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新菜单")]
    public async Task UpdateMenu(UpdateMenuInput input)
    {
        if (input.Id == input.Pid)
            throw Oops.Oh(ErrorCodeEnum.D4008);

        var isExist = input.Type != MenuTypeEnum.Btn
            ? await _sysMenuRep.IsAnyAsync(u => u.Title == input.Title && u.Type == input.Type && u.Pid == input.Pid && u.Id != input.Id)
            : await _sysMenuRep.IsAnyAsync(u => u.Pid == input.Pid && u.Permission == input.Permission && u.Id != input.Id);
        if (isExist)
            throw Oops.Oh(ErrorCodeEnum.D4000);

        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            if (await _sysMenuRep.IsAnyAsync(u => u.Id != input.Id && u.Name == input.Name))
                throw Oops.Oh(ErrorCodeEnum.D4009);
        }

        if (input.Pid != 0)
        {
            if (await _sysMenuRep.IsAnyAsync(u => u.Id == input.Pid && u.Type == MenuTypeEnum.Btn))
                throw Oops.Oh(ErrorCodeEnum.D4010);
        }

        // 校验菜单参数
        var sysMenu = input.Adapt<SysMenu>();
        CheckMenuParam(sysMenu);

        await _sysMenuRep.AsUpdateable(sysMenu).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除菜单 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除菜单")]
    public async Task DeleteMenu(DeleteMenuInput input)
    {
        var menuTreeList = await _sysMenuRep.AsQueryable().ToChildListAsync(u => u.Pid, input.Id, true);
        var menuIdList = menuTreeList.Select(u => u.Id).ToList();

        await _sysMenuRep.DeleteAsync(u => menuIdList.Contains(u.Id));

        // 级联删除角色菜单数据
        await _sysRoleMenuService.DeleteRoleMenuByMenuIdList(menuIdList);

        // 级联删除用户收藏菜单
        await _sysUserMenuService.DeleteMenuList(menuIdList);
    }

    /// <summary>
    /// 增加和编辑时检查菜单数据
    /// </summary>
    /// <param name="menu"></param>
    private static void CheckMenuParam(SysMenu menu)
    {
        var permission = menu.Permission;
        if (menu.Type == MenuTypeEnum.Btn)
        {
            menu.Name = null;
            menu.Path = null;
            menu.Component = null;
            menu.Icon = null;
            menu.Redirect = null;
            menu.OutLink = null;
            menu.IsHide = false;
            menu.IsKeepAlive = true;
            menu.IsAffix = false;
            menu.IsIframe = false;

            if (string.IsNullOrEmpty(permission))
                throw Oops.Oh(ErrorCodeEnum.D4003);
            if (!permission.Contains('/'))
                throw Oops.Oh(ErrorCodeEnum.D4004);
        }
        else
        {
            menu.Permission = null;
        }
    }

    /// <summary>
    /// 获取当前用户菜单Id集合
    /// </summary>
    /// <returns></returns>
    private async Task<List<long>> GetMenuIdList()
    {
        var roleIdList = await _sysUserRoleService.GetUserRoleIdList(_userManager.UserId);
        return await _sysRoleMenuService.GetRoleMenuIdList(roleIdList);
    }
}