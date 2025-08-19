// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Furion.Localization;

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统报表数据源服务
/// </summary>
[ApiDescriptionSettings(Order = 245, Description = "报表数据源")]
public class SysReportDataSourceService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysReportDataSource> _reportDataSourceRep;
    private readonly SqlSugarRepository<SysTenant> _tenantRep;
    private readonly ISqlSugarClient _db;

    public SysReportDataSourceService(SqlSugarRepository<SysReportDataSource> reportDataSourceRep,
        SqlSugarRepository<SysTenant> tenantRep,
        ISqlSugarClient db
    )
    {
        _reportDataSourceRep = reportDataSourceRep;
        _tenantRep = tenantRep;
        _db = db;
    }

    /// <summary>
    /// 获取报表数据源分页列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取报表数据源分页列表")]
    public async Task<SqlSugarPagedList<SysReportDataSource>> Page(PageReportDataSourceInput input)
    {
        var list = await _reportDataSourceRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name.Trim()))
            .Select(u => new SysReportDataSource(), true)
            .OrderBuilder(input)
            .ToPagedListAsync(input.Page, input.PageSize);

        // // 清空连接字符串
        // foreach (var item in list.Items) item.ConnectionString = "";

        return list;
    }

    /// <summary>
    /// 增加报表数据源
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加报表数据源")]
    public async Task Add(AddReportDataSourceInput input)
    {
        var isExist = await _reportDataSourceRep.IsAnyAsync(u => u.Name == input.Name && u.Id != input.Id);
        if (isExist)
            throw Oops.Bah(ErrorCodeEnum.C1000);

        await _reportDataSourceRep.InsertAsync(input.Adapt<SysReportDataSource>());
    }

    /// <summary>
    /// 更新报表数据源
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新报表数据源")]
    public async Task Update(UpdateReportDataSourceInput input)
    {
        var isExist = await _reportDataSourceRep.IsAnyAsync(u => u.Name == input.Name && u.Id != input.Id);
        if (isExist)
            throw Oops.Bah(ErrorCodeEnum.C1000);

        var updateEntity = input.Adapt<SysReportDataSource>();

        // // 如果连接字符串为空，则使用原来的值
        // if (string.IsNullOrEmpty(updateEntity.ConnectionString))
        // {
        //     var entity = await _rep.GetFirstAsync(u => u.Id == input.Id);
        //     updateEntity.ConnectionString = entity.ConnectionString;
        // }

        await _reportDataSourceRep.UpdateAsync(updateEntity);
    }

    /// <summary>
    /// 删除报表数据源
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除报表数据源")]
    public async Task Delete(BaseIdInput input)
    {
        await _reportDataSourceRep.DeleteAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取包含详细信息的数据源列表
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<SysReportDataSourceDetail>> GetDataSourceListIncludeDetail()
    {
        var list = new List<SysReportDataSourceDetail>();

        // 从配置获取数据源
        var configs = App.GetOptions<DbConnectionOptions>().ConnectionConfigs;
        foreach (var config in configs)
        {
            var configId = config.ConfigId.ToString();
            var db = _db.AsTenant().GetConnectionScope(configId);
            list.Add(new SysReportDataSourceDetail
            {
                Id = configId,
                Name = db.Ado.Connection.Database,
                IsBuildIn = true,
                DbType = config.DbType,
                ConnectionString = db.Ado.Connection.ConnectionString,
            });
        }

        // 从租户获取数据源
        var tenantList = await _tenantRep.AsQueryable()
            .LeftJoin<SysOrg>((u, o) => u.OrgId == o.Id).ClearFilter()
            .Where((u, o) => u.TenantType == TenantTypeEnum.Db)
            .Select((u, o) => new { u.Id, o.Name, u.DbType, u.Connection })
            .ToListAsync();
        foreach (var tenant in tenantList)
        {
            list.Add(new SysReportDataSourceDetail
            {
                Id = tenant.Id.ToString(),
                Name = tenant.Name,
                IsBuildIn = true,
                DbType = tenant.DbType,
                ConnectionString = tenant.Connection,
            });
        }

        // 用户自定义
        var dsList = await _reportDataSourceRep.GetListAsync();
        foreach (var ds in dsList)
        {
            list.Add(new SysReportDataSourceDetail
            {
                Id = ds.Id.ToString(),
                Name = ds.Name,
                IsBuildIn = false,
                DbType = ds.DbType,
                ConnectionString = ds.ConnectionString,
            });
        }

        foreach (var item in list.Where(item => item.IsBuildIn)) item.Name += L.Text["(内置)"];

        return list;
    }

    /// <summary>
    /// 获取报表数据源列表
    /// </summary>
    [DisplayName("获取报表数据源列表")]
    public async Task<List<ReportDataSourceOutput>> GetDataSourceList()
    {
        return (await GetDataSourceListIncludeDetail()).Select(u => new ReportDataSourceOutput
        {
            Id = u.Id,
            Name = u.Name,
            IsBuildIn = u.IsBuildIn
        }).ToList();
    }
}