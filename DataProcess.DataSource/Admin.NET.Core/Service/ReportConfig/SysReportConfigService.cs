// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using OfficeOpenXml;

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统报表配置服务
/// </summary>
[ApiDescriptionSettings(Order = 245, Description = "报表配置")]
public class SysReportConfigService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysReportConfig> _reportConfigRep;
    private readonly SysReportDataSourceService _sysReportDataSourceService;
    private readonly SysTenantService _sysTenantService;
    private readonly UserManager _userManager;

    public SysReportConfigService(SqlSugarRepository<SysReportConfig> reportConfigRep,
        SysReportDataSourceService sysReportDataSourceService,
        SysTenantService sysTenantService,
        UserManager userManager
    )
    {
        _reportConfigRep = reportConfigRep;
        _sysReportDataSourceService = sysReportDataSourceService;
        _sysTenantService = sysTenantService;
        _userManager = userManager;
    }

    /// <summary>
    /// 获取报表配置分页列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取报表配置分页列表")]
    public async Task<SqlSugarPagedList<ReportConfigOutput>> Page(PageReportConfigInput input)
    {
        return await _reportConfigRep.AsQueryable()
            .LeftJoin<SysReportGroup>((u, a) => u.GroupId == a.Id)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), (u, a) => u.Name.Contains(input.Name.Trim()))
            .WhereIF(input.GroupId is > 0, (u, a) => u.GroupId == input.GroupId)
            .Select((u, a) => new ReportConfigOutput
            {
                GroupName = a.Name
            }, true)
            .OrderBuilder(input, "u.")
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加报表配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加报表配置")]
    public async Task Add(AddReportConfigInput input)
    {
        var isExist = await _reportConfigRep.IsAnyAsync(u => u.Name == input.Name && u.Id != input.Id);
        if (isExist)
            throw Oops.Bah(ErrorCodeEnum.C1000);

        await _reportConfigRep.InsertAsync(input.Adapt<SysReportConfig>());
    }

    /// <summary>
    /// 更新报表配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新报表配置")]
    public async Task Update(UpdateReportConfigInput input)
    {
        var isExist = await _reportConfigRep.IsAnyAsync(u => u.Name == input.Name && u.Id != input.Id);
        if (isExist)
            throw Oops.Bah(ErrorCodeEnum.C1000);

        await _reportConfigRep.UpdateAsync(input.Adapt<SysReportConfig>());
    }

    /// <summary>
    /// 删除报表配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除报表配置")]
    public async Task Delete(BaseIdInput input)
    {
        await _reportConfigRep.DeleteAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 复制报表配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Copy"), HttpPost]
    [DisplayName("复制报表配置")]
    public async Task Copy(BaseIdInput input)
    {
        var entity = await _reportConfigRep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Bah(ErrorCodeEnum.D1002);
        entity.Id = YitIdHelper.NextId();
        entity.Name = $"{entity.Name} - 副本";
        entity.CreateTime = DateTime.Now;
        entity.CreateUserId = null;
        entity.CreateUserName = null;
        entity.UpdateTime = null;
        entity.UpdateUserId = null;
        entity.UpdateUserName = null;

        await Add(entity.Adapt<AddReportConfigInput>());
    }

    /// <summary>
    /// 获取报表布局配置
    /// </summary>
    /// <param name="input">输入参数</param>
    [ApiDescriptionSettings(Name = "GetLayoutConfig"), HttpGet]
    [DisplayName("获取报表布局配置")]
    public SysReportLayoutConfig GetLayoutConfig([FromQuery] BaseIdInput input)
    {
        var entity = _reportConfigRep.GetFirst(u => u.Id == input.Id);
        return entity == null
            ? throw Oops.Bah(ErrorCodeEnum.D1002)
            : new SysReportLayoutConfig
            {
                Fields = string.IsNullOrEmpty(entity.Fields) ? [] : JSON.Deserialize<List<SysReportField>>(entity.Fields),
                Params = string.IsNullOrEmpty(entity.Params) ? [] : JSON.Deserialize<List<SysReportParam>>(entity.Params),
            };
    }

    /// <summary>
    /// 解析报表配置Sql
    /// </summary>
    /// <param name="input">输入参数</param>
    [ApiDescriptionSettings(Name = "ParseSql"), HttpPost]
    [DisplayName("解析报表配置Sql")]
    public async Task<ReportConfigParseSqlOutput> ParseSql(ReportConfigParseSqlInput input)
    {
        var dataTable = await InnerExecuteSqlScript(input.DataSource, input.SqlScript, input.ExecParams);
        var fieldNames = (from DataColumn column in dataTable.Columns select column.ColumnName).ToList();

        return new ReportConfigParseSqlOutput
        {
            FieldNames = fieldNames
        };
    }

    /// <summary>
    /// 执行报表配置Sql脚本
    /// </summary>
    /// <param name="input">输入参数</param>
    [ApiDescriptionSettings(Name = "ExecuteSqlScript"), HttpPost]
    [DisplayName("执行报表配置Sql脚本")]
    // 指定使用 CamelCaseDictionaryKey 的序列化配置选项，使字典的 Key 转换为驼峰样式
    [UnifySerializerSetting("CamelCaseDictionaryKey")]
    public async Task<List<Dictionary<string, object>>> ExecuteSqlScript(ReportConfigExecuteSqlScriptInput input)
    {
        var entity = await _reportConfigRep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Bah(ErrorCodeEnum.D1002);
        if (entity.DsType != ReportConfigDsTypeEnum.Sql)
            throw Oops.Bah(ErrorCodeEnum.C1001);

        var layoutConfig = GetLayoutConfig(input);
        var execParams = new Dictionary<string, object>(input.ExecParams);
        // 补充没有传入的参数，如果传入 null，则填充空字符串，不为 null sqlsugar 才会构造参数
        foreach (var param in layoutConfig.Params)
        {
            execParams.TryAdd(param.ParamName, "");
            execParams[param.ParamName] ??= "";
        }

        var dataTable = await InnerExecuteSqlScript(entity.DataSource, entity.SqlScript, execParams);

        // 处理汇总字段
        var summaryFieldNames = layoutConfig.Fields.Where(u => u.IsSummary).Select(u => u.FieldName).ToList();
        if (summaryFieldNames.Count > 0)
        {
            var summaryInfo = new Dictionary<string, decimal>();
            UnifyContext.Fill(summaryInfo);
            foreach (var summaryFieldName in summaryFieldNames)
            {
                summaryInfo[summaryFieldName] = dataTable.AsEnumerable().Sum(row =>
                {
                    decimal.TryParse(row[summaryFieldName] + "", out decimal summaryValue);
                    return summaryValue;
                });
            }
        }

        return dataTable.AsEnumerable().Select(UtilMethods.DataRowToDictionary).ToList();
    }

    /// <summary>
    /// 执行Sql脚本内部实现
    /// </summary>
    /// <param name="dataSource"></param>
    /// <param name="sqlScript"></param>
    /// <param name="execParams"></param>
    /// <returns></returns>
    private async Task<DataTable> InnerExecuteSqlScript(string dataSource, string sqlScript, Dictionary<string, object> execParams)
    {
        var isSelectQuery = IsSelectQuery(sqlScript);

        var dataSourceDetailList = await _sysReportDataSourceService.GetDataSourceListIncludeDetail();
        var dataSourceDetail = dataSourceDetailList.FirstOrDefault(u => u.Id == dataSource) ?? throw Oops.Bah(ErrorCodeEnum.C1002);
        ISqlSugarClient dbClient = GetDbClient(dataSourceDetail);

        var newExecParams = BuildInParamsHandle(dbClient, sqlScript, execParams);
        var parameters = newExecParams.Select(u => new SugarParameter(u.Key, u.Value)).ToList();

        if (isSelectQuery)
        {
            // SELECT 查询
            var dataTable = await dbClient.Ado.GetDataTableAsync(sqlScript, parameters);
            return dataTable;
        }
        else
        {
            // 存储过程
            var dataTable = await dbClient.Ado.UseStoredProcedure().GetDataTableAsync(sqlScript, parameters);
            return dataTable;
        }
    }

    /// <summary>
    /// 获取 DbClient
    /// </summary>
    /// <param name="dataSourceDetail"></param>
    /// <returns></returns>
    private ISqlSugarClient GetDbClient(SysReportDataSourceDetail dataSourceDetail)
    {
        ISqlSugarClient dbClient = null;
        if (dataSourceDetail.IsBuildIn)
        {
            // 获取内置数据库和租户的连接
            dbClient = _sysTenantService.GetTenantDbConnectionScope(long.Parse(dataSourceDetail.Id));
        }
        else
        {
            if (SqlSugarSetup.ITenant.IsAnyConnection(dataSourceDetail.Id))
            {
                dbClient = SqlSugarSetup.ITenant.GetConnectionScope(dataSourceDetail.Id);
            }

            // dbClient 不存在或者连接字符串不一样，则重新设置
            if (dbClient == null || dbClient.CurrentConnectionConfig.ConnectionString != dataSourceDetail.ConnectionString)
            {
                // 获取默认库连接配置
                var dbOptions = App.GetOptions<DbConnectionOptions>();
                var mainConnConfig = dbOptions.ConnectionConfigs.First(u => u.ConfigId.ToString() == SqlSugarConst.MainConfigId);

                // 设置连接配置
                var tenantConnConfig = new DbConnectionConfig
                {
                    ConfigId = dataSourceDetail.Id,
                    DbType = dataSourceDetail.DbType,
                    IsAutoCloseConnection = true,
                    ConnectionString = dataSourceDetail.ConnectionString,
                    DbSettings = new DbSettings
                    {
                        EnableUnderLine = mainConnConfig.DbSettings.EnableUnderLine,
                    },
                };
                SqlSugarSetup.ITenant.AddConnection(tenantConnConfig);
                dbClient = SqlSugarSetup.ITenant.GetConnectionScope(dataSourceDetail.Id);
            }
        }

        return dbClient;
    }

    /// <summary>
    /// 内置参数处理
    /// </summary>
    private Dictionary<string, object> BuildInParamsHandle(ISqlSugarClient dbClient, string sqlScript, Dictionary<string, object> execParams)
    {
        var newExecParams = new Dictionary<string, object>(execParams);

        if (sqlScript.IndexOf("@curTenantId", StringComparison.Ordinal) > -1) newExecParams["@curTenantId"] = _userManager.TenantId;
        if (sqlScript.IndexOf("@curUserId", StringComparison.Ordinal) > -1) newExecParams["@curUserId"] = _userManager.UserId;

        return newExecParams;
    }

    private static bool IsSelectQuery(string sqlScript)
    {
        // 使用正则表达式判断是否包含 SELECT 关键字（忽略大小写）
        var pattern = @"\bselect\b";
        var isSelectQuery = Regex.IsMatch(sqlScript, pattern, RegexOptions.IgnoreCase);

        return isSelectQuery;
    }

    /// <summary>
    /// 导出报表到Excel
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="AppFriendlyException"></exception>
    [ApiDescriptionSettings(Name = "ExportToExcel"), HttpPost]
    [DisplayName("导出报表到Excel")]
    public async Task<FileStreamResult> ExportToExcel(ReportConfigExecuteSqlScriptInput input)
    {
        var entity = await _reportConfigRep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Bah(ErrorCodeEnum.D1002);

        // 执行Sql脚本
        var data = await ExecuteSqlScript(input);
        // 重新创建忽略大小写的字典
        data = data.Select(u => new Dictionary<string, object>(u, StringComparer.OrdinalIgnoreCase)).ToList();

        var layoutConfig = GetLayoutConfig(input);
        var fields = layoutConfig.Fields.Where(f => f.Visible).ToList();

        // 按字段原始顺序处理分组
        var orderedGroups = OrderedGroupFields(fields);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("ReportData");

        int currentRow = 1;
        int startCol = 1;
        if (orderedGroups.Any(u => u.Any(p => !string.IsNullOrEmpty(p.GroupTitle))))
        {
            foreach (var group in orderedGroups)
            {
                int colCount = group.Count();
                worksheet.Cells[currentRow, startCol, currentRow, startCol + colCount - 1].Merge = true;
                worksheet.Cells[currentRow, startCol].Value = group.First().GroupTitle;
                startCol += colCount;
            }

            currentRow++;
        }

        // 处理列标题（使用Title或FieldName）
        var curColIndex = 0;
        foreach (var field in orderedGroups.SelectMany(group => group))
        {
            worksheet.Cells[currentRow, curColIndex + 1].Value = string.IsNullOrEmpty(field.Title) ? field.FieldName : field.Title;
            curColIndex++;
        }

        currentRow++;

        // 填充数据
        foreach (var item in data)
        {
            curColIndex = 0;
            foreach (var field in orderedGroups.SelectMany(group => group))
            {
                worksheet.Cells[currentRow, curColIndex + 1].Value = item[field.FieldName];
                curColIndex++;
            }

            currentRow++;
        }

        // 处理汇总行
        var summaryFields = fields.Where(f => f.IsSummary).ToList();
        if (summaryFields.Count > 0)
        {
            worksheet.Cells[currentRow, 1].Value = "汇总";
            foreach (var field in summaryFields)
            {
                int colIndex = fields.FindIndex(f => f.FieldName == field.FieldName) + 1;
                decimal sum = data.Sum(r =>
                {
                    decimal.TryParse(r[field.FieldName]?.ToString(), out decimal val);
                    return val;
                });
                worksheet.Cells[currentRow, colIndex].Value = sum;
            }

            currentRow++;
        }

        // 自动调整列宽
        worksheet.Cells[1, 1, currentRow - 1, fields.Count].AutoFitColumns();

        var stream = new MemoryStream();
        package.SaveAs(stream);
        stream.Position = 0;

        var fileName = entity.Name + $"_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = fileName };
    }

    /// <summary>
    /// 按照字段原始顺序处理分组
    /// </summary>
    private List<IGrouping<string, SysReportField>> OrderedGroupFields(List<SysReportField> fields)
    {
        // GroupTitle 没有值，填充特定值作为独自分组
        foreach (var field in fields.Where(field => string.IsNullOrWhiteSpace(field.GroupTitle)))
            field.GroupTitle = $"-{field.FieldName}-";

        // 按分组标题分组
        var groupFields = fields.GroupBy(field => field.GroupTitle).ToList();

        // 按字段原始顺序处理分组
        var orderedGroups = new List<IGrouping<string, SysReportField>>();

        foreach (var field in fields)
        {
            var group = groupFields.First(g => g.Key == field.GroupTitle);
            if (orderedGroups.Any(u => u.Key == group.Key)) continue;

            orderedGroups.Add(group);
        }

        // 还原 GroupTitle 为空
        foreach (var field in fields.Where(field => field.GroupTitle == $"-{field.FieldName}-"))
            field.GroupTitle = "";

        return orderedGroups;
    }
}