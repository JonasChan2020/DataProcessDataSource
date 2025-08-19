// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统更新日志服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 380, Description = "更新日志")]
public class SysUpgradeService : IDynamicApiController, ITransient
{
    private readonly UserManager _userManager;
    private readonly SqlSugarRepository<SysUpgrade> _sysUpgradeRep;
    private readonly SqlSugarRepository<SysUpgradeUser> _sysUpgradeUserRep;
    private readonly SysConfigService _sysConfigService;

    public SysUpgradeService(UserManager userManager,
        SqlSugarRepository<SysUpgrade> sysUpgradeRep,
        SqlSugarRepository<SysUpgradeUser> sysUpgradeUserRep,
        SysConfigService sysConfigService)
    {
        _userManager = userManager;
        _sysUpgradeRep = sysUpgradeRep;
        _sysUpgradeUserRep = sysUpgradeUserRep;
        _sysConfigService = sysConfigService;
    }

    /// <summary>
    /// 获取系统更新日志分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取系统更新日志分页列表")]
    public async Task<SqlSugarPagedList<SysUpgrade>> Page(PageUpgradeInput input)
    {
        return await _sysUpgradeRep.AsQueryable()
            .OrderBy(u => u.CreateTime, OrderByType.Desc)
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加系统更新日志 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加系统更新日志")]
    public async Task AddUpgrade(SysUpgrade input)
    {
        var upgrade = input.Adapt<SysUpgrade>();
        await _sysUpgradeRep.InsertAsync(upgrade);
    }

    /// <summary>
    /// 更新系统更新日志 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新系统更新日志")]
    public async Task UpdateUpgrade(SysUpgrade input)
    {
        var upgrade = input.Adapt<SysUpgrade>();
        await _sysUpgradeRep.UpdateAsync(upgrade);
    }

    /// <summary>
    /// 删除系统更新日志 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除系统更新日志")]
    public async Task DeleteUpgrade(BaseIdInput input)
    {
        await _sysUpgradeRep.DeleteAsync(u => u.Id == input.Id);

        await _sysUpgradeUserRep.DeleteAsync(u => u.UpgradeId == input.Id);
    }

    /// <summary>
    /// 设置系统更新日志已读状态 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("设置系统更新日志已读状态")]
    public async Task SetRead(BaseIdInput input)
    {
        await _sysUpgradeUserRep.InsertAsync(new SysUpgradeUser
        {
            UpgradeId = input.Id,
            UserId = _userManager.UserId,
            ReadTime = DateTime.Now
        });
    }

    /// <summary>
    /// 获取最新的系统更新日志 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取最新的系统更新日志")]
    public async Task<SysUpgrade> GetLastUnRead()
    {
        // 是否启用显示系统更新日志
        var enableUpgrade = await _sysConfigService.GetConfigValueByCode<bool>(ConfigConst.SysUpgrade);
        if (!enableUpgrade) return null;

        // 取最新的系统更新日志
        var upgrade = await _sysUpgradeRep.AsQueryable().OrderBy(u => u.CreateTime, OrderByType.Desc).FirstAsync();
        if (upgrade == null) return null;

        // 若当前用户没有阅读过则进行显示
        return (await _sysUpgradeUserRep.IsAnyAsync(u => u.UserId == _userManager.UserId && u.UpgradeId == upgrade.Id)) ? null : upgrade;
    }
}