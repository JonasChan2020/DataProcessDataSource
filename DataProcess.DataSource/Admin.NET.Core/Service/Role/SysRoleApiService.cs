// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统角色接口黑名单服务
/// </summary>
public class SysRoleApiService : ITransient
{
    private readonly SqlSugarRepository<SysRoleApi> _sysRoleApiRep;

    public SysRoleApiService(SqlSugarRepository<SysRoleApi> sysRoleApiRep)
    {
        _sysRoleApiRep = sysRoleApiRep;
    }

    /// <summary>
    /// 授权角色接口
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task GrantRoleApi(RoleApiInput input)
    {
        await _sysRoleApiRep.DeleteAsync(u => u.RoleId == input.Id);

        if (input.ApiList == null || input.ApiList.Count < 1)
            return;

        var roleApis = input.ApiList.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => new SysRoleApi
        {
            RoleId = input.Id,
            Route = u
        }).ToList();
        await _sysRoleApiRep.InsertRangeAsync(roleApis);
    }

    /// <summary>
    /// 根据角色Id集合获取接口集合
    /// </summary>
    /// <param name="roleIdList"></param>
    /// <returns></returns>
    public async Task<List<string>> GetRoleApiList(List<long> roleIdList)
    {
        return await _sysRoleApiRep.AsQueryable()
            .WhereIF(roleIdList != null, u => roleIdList.Contains(u.RoleId))
            .Select(u => u.Route).ToListAsync();
    }

    /// <summary>
    /// 根据角色Id删除接口
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    public async Task DeleteRoleApiByRoleId(long roleId)
    {
        await _sysRoleApiRep.DeleteAsync(u => u.RoleId == roleId);
    }

    /// <summary>
    /// 根据角色Id复制角色接口
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="newRoleId"></param>
    /// <returns></returns>
    public async Task CopyRoleApiByRoleId(long roleId, long newRoleId)
    {
        var roleApiList = await _sysRoleApiRep.GetListAsync(u => u.RoleId == roleId);
        roleApiList.ForEach(u =>
        {
            u.Id = 0;
            u.RoleId = newRoleId;
        });
        await _sysRoleApiRep.InsertRangeAsync(roleApiList);
    }
}