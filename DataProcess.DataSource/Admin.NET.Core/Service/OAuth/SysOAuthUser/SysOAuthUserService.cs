// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统OAuth账号服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 488, Description = "OAuth账号")]
public class SysOAuthUserService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysOAuthUser> _sysOAuthUserRep;

    public SysOAuthUserService(SqlSugarRepository<SysOAuthUser> sysOAuthUserRep)
    {
        _sysOAuthUserRep = sysOAuthUserRep;
    }

    /// <summary>
    /// 获取OAuth账号列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取OAuth账号列表")]
    public async Task<SqlSugarPagedList<OAuthUserOutput>> Page(OAuthUserInput input)
    {
        return await _sysOAuthUserRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Account), u => u.Account.Contains(input.Account))
            .WhereIF(!string.IsNullOrWhiteSpace(input.NickName), u => u.NickName.Contains(input.NickName))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Mobile), u => u.Mobile.Contains(input.Mobile))
            .OrderBy(u => u.Id, OrderByType.Desc)
            .Select<OAuthUserOutput>()
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加OAuth账号 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加OAuth账号")]
    public async Task AddWechatUser(SysOAuthUser input)
    {
        await _sysOAuthUserRep.InsertAsync(input.Adapt<SysOAuthUser>());
    }

    /// <summary>
    /// 更新OAuth账号 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新OAuth账号")]
    public async Task UpdateWechatUser(SysOAuthUser input)
    {
        var weChatUser = input.Adapt<SysOAuthUser>();
        await _sysOAuthUserRep.AsUpdateable(weChatUser).IgnoreColumns(true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除OAuth账号 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除OAuth账号")]
    public async Task DeleteWechatUser(DeleteOAuthUserInput input)
    {
        await _sysOAuthUserRep.DeleteByIdAsync(input.Id);
    }
}