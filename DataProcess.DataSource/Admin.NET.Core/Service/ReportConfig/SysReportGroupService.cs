// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统报表分组服务
/// </summary>
[ApiDescriptionSettings(Order = 245, Description = "报表分组")]
public class SysReportGroupService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysReportGroup> _reportGroupRep;

    public SysReportGroupService(SqlSugarRepository<SysReportGroup> reportGroupRep)
    {
        _reportGroupRep = reportGroupRep;
    }

    /// <summary>
    /// 获取报表分组列表
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取报表分组列表")]
    public async Task<List<SysReportGroup>> GetList()
    {
        return await _reportGroupRep.AsQueryable().OrderBy(u => u.Number).ToListAsync();
    }

    /// <summary>
    /// 增加报表分组
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加报表分组")]
    public async Task Add(AddReportGroupInput input)
    {
        var isExist = await _reportGroupRep.IsAnyAsync(u => u.Number == input.Number && u.Id != input.Id);
        if (isExist)
            throw Oops.Bah(ErrorCodeEnum.C1003);

        await _reportGroupRep.InsertAsync(input.Adapt<SysReportGroup>());
    }

    /// <summary>
    /// 更新报表分组
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新报表分组")]
    public async Task Update(UpdateReportGroupInput input)
    {
        var isExist = await _reportGroupRep.IsAnyAsync(u => u.Number == input.Number && u.Id != input.Id);
        if (isExist)
            throw Oops.Bah(ErrorCodeEnum.C1003);

        await _reportGroupRep.UpdateAsync(input.Adapt<SysReportGroup>());
    }

    /// <summary>
    /// 删除报表分组
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除报表分组")]
    public async Task Delete(BaseIdInput input)
    {
        await _reportGroupRep.DeleteAsync(u => u.Id == input.Id);
    }
}