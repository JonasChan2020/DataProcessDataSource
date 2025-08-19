// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.IO.Compression;
using DbType = SqlSugar.DbType;

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统代码生成器服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 270, Description = "代码生成器")]
public class SysCodeGenService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly CodeGenOptions _codeGenOptions;
    private readonly DbConnectionOptions _dbConnectionOptions;
    private readonly SysCodeGenConfigService _codeGenConfigService;
    private readonly IViewEngine _viewEngine;

    public SysCodeGenService(ISqlSugarClient db,
        IOptions<CodeGenOptions> codeGenOptions,
        IOptions<DbConnectionOptions> dbConnectionOptions,
        SysCodeGenConfigService codeGenConfigService,
        IViewEngine viewEngine)
    {
        _db = db;
        _dbConnectionOptions = dbConnectionOptions.Value;
        _codeGenOptions = codeGenOptions.Value;
        _codeGenConfigService = codeGenConfigService;
        _viewEngine = viewEngine;
    }

    /// <summary>
    /// 获取代码生成分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取代码生成分页列表")]
    public async Task<SqlSugarPagedList<SysCodeGen>> Page(PageCodeGenInput input)
    {
        return await _db.Queryable<SysCodeGen>()
            .Includes(u => u.CodeGenTemplateRelations)
            .WhereIF(!string.IsNullOrWhiteSpace(input.TableName), u => u.TableName!.Contains(input.TableName.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.BusName), u => u.BusName!.Contains(input.BusName.Trim()))
            .OrderBy(u => u.Id, OrderByType.Desc)
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加代码生成 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加代码生成")]
    public async Task AddCodeGen(AddCodeGenInput input)
    {
        var isExist = await _db.Queryable<SysCodeGen>().Where(u => u.TableName == input.TableName).AnyAsync();
        if (isExist)
            throw Oops.Oh(ErrorCodeEnum.D1400);

        var codeGen = input.Adapt<SysCodeGen>();
        long id = YitIdHelper.NextId();
        var templateRelations = SysCodeGenService.GetCodeGenTemplateRelation(id, input.CodeGenTemplateIds);
        codeGen.Id = id;
        codeGen.CodeGenTemplateRelations = templateRelations;
        //var newCodeGen = await _db.Insertable(codeGen).ExecuteReturnEntityAsync();
        var newCodeGen = await _db.InsertNav(codeGen)
            .Include(t => t.CodeGenTemplateRelations)
            .ExecuteReturnEntityAsync();

        // 增加配置表
        _codeGenConfigService.AddList(_codeGenConfigService.GetColumnList(input), newCodeGen);
    }

    /// <summary>
    /// 获取代码生成模板关系集合 🔖
    /// </summary>
    /// <param name="codeGenId"></param>
    /// <param name="templateIds"></param>
    /// <returns></returns>
    private static List<SysCodeGenTemplateRelation> GetCodeGenTemplateRelation(long codeGenId, List<long> templateIds)
    {
        List<SysCodeGenTemplateRelation> list = [];
        foreach (var item in templateIds)
        {
            SysCodeGenTemplateRelation relation = new()
            {
                CodeGenId = codeGenId,
                TemplateId = item
            };
            list.Add(relation);
        }
        return list;
    }

    /// <summary>
    /// 更新代码生成 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新代码生成")]
    public async Task UpdateCodeGen(UpdateCodeGenInput input)
    {
        //开发阶段不断生成调整
        //var isExist = await _db.Queryable<SysCodeGen>().AnyAsync(u => u.TableName == input.TableName && u.Id != input.Id);
        //if (isExist)
        //    throw Oops.Oh(ErrorCodeEnum.D1400);

        var codeGen = input.Adapt<SysCodeGen>();
        var templateRelations = SysCodeGenService.GetCodeGenTemplateRelation(codeGen.Id, input.CodeGenTemplateIds);
        codeGen.CodeGenTemplateRelations = templateRelations;
        //await _db.Updateable(codeGen).ExecuteCommandAsync();
        await _db.UpdateNav(codeGen).Include(t => t.CodeGenTemplateRelations).ExecuteCommandAsync();

        // 更新配置表
        _codeGenConfigService.AddList(_codeGenConfigService.GetColumnList(input.Adapt<AddCodeGenInput>()), codeGen);
    }

    /// <summary>
    /// 删除代码生成 🔖
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除代码生成")]
    public async Task DeleteCodeGen(List<DeleteCodeGenInput> inputs)
    {
        if (inputs == null || inputs.Count < 1) return;

        var codeGenConfigTaskList = new List<Task>();
        inputs.ForEach(u =>
        {
            //_db.Deleteable<SysCodeGen>().In(u.Id).ExecuteCommand();
            _db.DeleteNav<SysCodeGen>(t => t.Id == u.Id)
               .Include(t => t.CodeGenTemplateRelations)
               .ExecuteCommand();

            // 删除配置表
            codeGenConfigTaskList.Add(_codeGenConfigService.DeleteCodeGenConfig(u.Id));
        });
        await Task.WhenAll(codeGenConfigTaskList);
    }

    /// <summary>
    /// 获取代码生成详情 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取代码生成详情")]
    public async Task<SysCodeGen> GetDetail([FromQuery] QueryCodeGenInput input)
    {
        return await _db.Queryable<SysCodeGen>().SingleAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取数据库库集合 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取数据库库集合")]
    public async Task<List<DatabaseOutput>> GetDatabaseList()
    {
        var dbConfigs = _dbConnectionOptions.ConnectionConfigs;
        return await Task.FromResult(dbConfigs.Adapt<List<DatabaseOutput>>());
    }

    /// <summary>
    /// 获取数据库表(实体)集合 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取数据库表(实体)集合")]
    public async Task<List<TableOutput>> GetTableList(string configId = SqlSugarConst.MainConfigId)
    {
        var provider = _db.AsTenant().GetConnectionScope(configId);
        var dbTableInfos = provider.DbMaintenance.GetTableInfoList(false);

        var config = _dbConnectionOptions.ConnectionConfigs.FirstOrDefault(u => configId.Equals(u.ConfigId));

        // 获取所有实体列表并按照程序集和实体名称进行排序
        IEnumerable<EntityInfo> entityInfos = await _codeGenConfigService.GetEntityInfos();
        entityInfos = entityInfos.OrderBy(u => u.Type.Assembly.ManifestModule.Name).ThenBy(u => u.EntityName);

        var tableOutputList = new List<TableOutput>();
        foreach (var item in entityInfos)
        {
            var tbConfigId = item.Type.GetCustomAttribute<TenantAttribute>()?.configId as string ?? SqlSugarConst.MainConfigId;
            if (item.Type.IsDefined(typeof(LogTableAttribute)) && !item.Type.IsDefined(typeof(SysTableAttribute))) tbConfigId = SqlSugarConst.LogConfigId;
            if (tbConfigId != configId)
                continue;

            var dbTableName = item.DbTableName;
            int bracketIndex = dbTableName.IndexOf('{');
            if (bracketIndex != -1)
                dbTableName = dbTableName.Substring(0, bracketIndex);

            var table = dbTableInfos.FirstOrDefault(u => u.Name.ToLower().Equals((config != null && config.DbSettings.EnableUnderLine ? UtilMethods.ToUnderLine(dbTableName) : dbTableName).ToLower()));
            //if (table == null) continue;
            tableOutputList.Add(new TableOutput
            {
                ConfigId = configId,
                EntityName = item.EntityName,
                TableName = table?.Name,
                TableComment = item.TableDescription,
                ColumnCount = item.Type.GetProperties().Length,
                AssemblyName = item.Type.Assembly.ManifestModule.Name
            });
        }
        return tableOutputList;
    }

    /// <summary>
    /// 根据表名获取列集合 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("根据表名获取列集合")]
    public List<ColumnOuput> GetColumnListByTableName([Required] string tableName, string configId = SqlSugarConst.MainConfigId)
    {
        // 切库---多库代码生成用
        var provider = _db.AsTenant().GetConnectionScope(configId);

        var config = _dbConnectionOptions.ConnectionConfigs.FirstOrDefault(u => u.ConfigId.ToString() == configId);
        // 获取实体类型属性
        var entityType = provider.DbMaintenance.GetTableInfoList(false).FirstOrDefault(u => u.Name == tableName);
        if (entityType == null) return null;
        var entityBasePropertyNames = _codeGenOptions.EntityBaseColumn[nameof(EntityTenantBaseData)];

        tableName = GetRealTableName(tableName);
        var properties = _codeGenConfigService.GetEntityInfos().Result.First(u => GetRealTableName(u.DbTableName).EqualIgnoreCase(tableName)).Type.GetProperties()
            .Where(u => u.GetCustomAttribute<SugarColumn>()?.IsIgnore == false).Select(u => new
            {
                PropertyName = u.Name,
                ColumnComment = u.GetCustomAttribute<SugarColumn>()?.ColumnDescription,
                ColumnName = GetRealColumnName(u.GetCustomAttribute<SugarColumn>()?.ColumnName ?? u.Name)
            }).ToList();

        // 按原始类型的顺序获取所有实体类型属性（不包含导航属性，会返回null）
        var columnList = provider.DbMaintenance.GetColumnInfosByTableName(entityType.Name).Select(u => new ColumnOuput
        {
            ColumnName = config!.DbSettings.EnableUnderLine ? CodeGenHelper.CamelColumnName(u.DbColumnName, entityBasePropertyNames) : u.DbColumnName,
            ColumnKey = u.IsPrimarykey.ToString(),
            DataType = u.DataType.ToString(),
            NetType = CodeGenHelper.ConvertDataType(u, provider.CurrentConnectionConfig.DbType),
            ColumnComment = u.ColumnDescription
        }).ToList();

        foreach (var column in columnList)
        {
            var property = properties.FirstOrDefault(u => u.ColumnName == column.ColumnName);
            column.ColumnComment ??= property?.ColumnComment;
            column.PropertyName = property?.PropertyName;
        }
        return columnList;

        string GetRealTableName(string name)
        {
            string realName = config!.DbSettings.EnableUnderLine ? UtilMethods.ToUnderLine(name) : name;
            return realName;
        }

        string GetRealColumnName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string realName = config!.DbSettings.EnableUnderLine ? CodeGenHelper.CamelColumnName(name, entityBasePropertyNames) : name;
            if (config.DbType == DbType.PostgreSQL) realName = realName.ToLower();
            return realName;
        }
    }

    /// <summary>
    /// 获取程序保存位置 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取程序保存位置")]
    public List<string> GetApplicationNamespaces()
    {
        return _codeGenOptions.BackendApplicationNamespaces;
    }

    /// <summary>
    /// 执行代码生成 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("执行代码生成")]
    public async Task<dynamic> RunLocal(SysCodeGen input)
    {
        if (string.IsNullOrEmpty(input.GenerateType)) input.GenerateType = "200";

        string outputPath = Path.Combine(App.WebHostEnvironment.WebRootPath, "codeGen", input.TableName!);
        if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true);

        var tableFieldList = await _codeGenConfigService.GetList(new CodeGenConfig { CodeGenId = input.Id }); // 字段集合
        SysCodeGenService.ProcessTableFieldList(tableFieldList); // 处理字段集合

        var queryWhetherList = tableFieldList.Where(u => u.QueryWhether == YesNoEnum.Y.ToString()).ToList(); // 前端查询集合
        var joinTableList = tableFieldList.Where(u => u.EffectType is "Upload" or "ForeignKey" or "ApiTreeSelector").ToList(); // 需要连表查询的字段
        var data = CreateCustomViewEngine(input, tableFieldList, queryWhetherList, joinTableList); // 创建视图引擎数据

        // 获取菜单
        var menuList = await GetMenus(input.TableName!, input.BusName!, input.MenuPid ?? 0, input.MenuIcon!, input.PagePath!, tableFieldList);
        if (input.GenerateMenu)
        {
            await AddMenu(menuList, input.MenuPid ?? 0); // 添加菜单
        }

        // 模板
        var templateList = GetTemplateList(input);
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "template");
        for (var i = 0; i < templateList.Count; i++)
        {
            string tResult = await ProcessTemplate(templateList[i], input, templatePath, data, menuList); // 处理模板
            string targetFile = templateList[i].OutputFile
                .Replace("{PagePath}", input.PagePath)
                .Replace("{TableName}", input.TableName)
                .Replace("{TableNameLower}", input.TableName?.ToFirstLetterLowerCase() ?? "");

            string tmpPath;
            if (!input.GenerateType.StartsWith('1'))
            {
                if (templateList[i].Type == CodeGenTypeEnum.Frontend)
                    tmpPath = Path.Combine(new DirectoryInfo(App.WebHostEnvironment.ContentRootPath).Parent!.Parent!.FullName, _codeGenOptions.FrontRootPath, "src");
                else
                    tmpPath = Path.Combine(new DirectoryInfo(App.WebHostEnvironment.ContentRootPath).Parent!.FullName, input.NameSpace!);
            }
            else
            {
                tmpPath = templateList[i].Type == CodeGenTypeEnum.Frontend ? Path.Combine(outputPath, _codeGenOptions.FrontRootPath, "src") : Path.Combine(outputPath, input!.NameSpace!);
            }
            targetFile = Path.Combine(tmpPath, targetFile);
            var dirPath = new DirectoryInfo(targetFile).Parent!.FullName;
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            await File.WriteAllTextAsync(targetFile, tResult, Encoding.UTF8);
        }

        // 非ZIP压缩返回空
        if (!input.GenerateType.StartsWith('1')) return null;

        var downloadPath = outputPath + ".zip";
        if (File.Exists(downloadPath)) File.Delete(downloadPath); // 删除同名文件
        ZipFile.CreateFromDirectory(outputPath, downloadPath);
        return new { url = $"{App.HttpContext.Request.Scheme}://{App.HttpContext.Request.Host.Value}/codeGen/{input.TableName}.zip" };
    }

    /// <summary>
    /// 获取代码生成预览 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取代码生成预览")]
    public async Task<Dictionary<string, string>> Preview(SysCodeGen input)
    {
        var tableFieldList = await _codeGenConfigService.GetList(new CodeGenConfig { CodeGenId = input.Id }); // 字段集合
        SysCodeGenService.ProcessTableFieldList(tableFieldList); // 处理字段集合

        var queryWhetherList = tableFieldList.Where(u => u.QueryWhether == YesNoEnum.Y.ToString()).ToList(); // 前端查询集合
        var joinTableList = tableFieldList.Where(u => u.EffectType is "Upload" or "ForeignKey" or "ApiTreeSelector").ToList(); // 需要连表查询的字段
        var data = CreateCustomViewEngine(input, tableFieldList, queryWhetherList, joinTableList); // 创建视图引擎数据

        // 获取模板文件并替换
        var templateList = GetTemplateList(input);
        var templatePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "template");

        await _db.Ado.BeginTranAsync();
        try
        {
            var menuList = await GetMenus(input.TableName!, input.BusName!, input.MenuPid ?? 0, input.MenuIcon!, input.PagePath!, tableFieldList);
            var result = new Dictionary<string, string>();
            foreach (var template in templateList)
            {
                string tResult = await ProcessTemplate(template, input, templatePath, data, menuList); // 处理模板
                result.Add(template.Name?.TrimEnd(".vm")!, tResult);
            }
            return result;
        }
        finally
        {
            await _db.Ado.RollbackTranAsync();
        }
    }

    /// <summary>
    /// 处理字段集合
    /// </summary>
    /// <param name="tableFieldList"></param>
    private static void ProcessTableFieldList(List<CodeGenConfig> tableFieldList)
    {
        foreach (var item in tableFieldList)
        {
            List<VerifyRuleItem> list = [];
            if (!string.IsNullOrWhiteSpace(item.Rules))
            {
                if (item.Rules != "[]")
                    list = JSON.Deserialize<List<VerifyRuleItem>>(item.Rules);
            }
            else
            {
                item.Rules = "[]";
            }
            item.RuleItems = list;
            item.WhetherRequired = list.Any(u => u.Type == "required") ? YesNoEnum.Y.ToString() : YesNoEnum.N.ToString();
            item.AnyRule = list.Count > 0;
            item.RemoteVerify = list.Any(u => u.Type == "remote");
        }
    }

    /// <summary>
    /// 创建视图引擎数据
    /// </summary>
    /// <param name="input"></param>
    /// <param name="tableFieldList"></param>
    /// <param name="queryWhetherList"></param>
    /// <param name="joinTableList"></param>
    /// <returns></returns>
    private CustomViewEngine CreateCustomViewEngine(SysCodeGen input, List<CodeGenConfig> tableFieldList, List<CodeGenConfig> queryWhetherList, List<CodeGenConfig> joinTableList)
    {
        return new CustomViewEngine(_db)
        {
            ConfigId = input.ConfigId!, // 库定位器名
            AuthorName = input.AuthorName!, // 作者
            BusName = input.BusName!, // 业务名称
            NameSpace = input.NameSpace!, // 命名空间
            ClassName = input.TableName!, // 类名称
            PagePath = input.PagePath!, // 页面目录
            ProjectLastName = input.NameSpace!.Split('.').Last(), // 项目最后个名称，生成的时候赋值
            QueryWhetherList = queryWhetherList, // 查询条件
            TableField = tableFieldList, // 表字段配置信息
            IsJoinTable = joinTableList.Count > 0, // 是否联表
            IsUpload = joinTableList.Any(u => u.EffectType == "Upload"), // 是否上传
            PrintType = input.PrintType!, // 支持打印类型
            PrintName = input.PrintName!, // 打印模板名称
            IsApiService = input.IsApiService,
            RemoteVerify = tableFieldList.Any(t => t.RemoteVerify), // 远程验证
            TreeName = input.TreeName,
            LowerTreeName = input.TreeName?.ToFirstLetterLowerCase() ?? "", // 首字母小写
            LeftTab = input.LeftTab,
            LeftKey = input.LeftKey!,
            LeftPrimaryKey = input.LeftPrimaryKey,
            LeftName = input.LeftName,
            LowerLeftTab = input.LeftTab?.ToFirstLetterLowerCase() ?? "", // 首字母小写
            LowerLeftKey = input.LeftKey?.ToFirstLetterLowerCase() ?? "", // 首字母小写
            LowerLeftPrimaryKey = input.LeftPrimaryKey?.ToFirstLetterLowerCase() ?? "", // 首字母小写
            BottomTab = input.BottomTab,
            BottomKey = input.BottomKey!,
            BottomPrimaryKey = input.BottomPrimaryKey,
            LowerBottomTab = input.BottomTab?.ToFirstLetterLowerCase() ?? "", // 首字母小写
            LowerBottomKey = input.BottomKey?.ToFirstLetterLowerCase() ?? "", // 首字母小写
            LowerBottomPrimaryKey = input.BottomPrimaryKey?.ToFirstLetterLowerCase() ?? "", // 首字母小写
            TabType = input.TabType ?? "",
        };
    }

    /// <summary>
    /// 处理模板
    /// </summary>
    /// <param name="template"></param>
    /// <param name="input"></param>
    /// <param name="templatePath"></param>
    /// <param name="data"></param>
    /// <param name="menuList"></param>
    /// <returns></returns>
    private async Task<string> ProcessTemplate(SysCodeGenTemplate template, SysCodeGen input, string templatePath, CustomViewEngine data, List<SysMenu> menuList)
    {
        string tResult;

        var filename = template.Name;
        // 更改默认首页模板
        if (filename == "web_views_index.vue.vm")
        {
            filename = string.IsNullOrEmpty(input.LeftTab) ? filename : "web_views_LeftTree.vue.vm"; // 左树右列表
            filename = (!string.IsNullOrEmpty(input.LeftTab) && !string.IsNullOrEmpty(input.BottomTab)) ? "web_views_BottomIndx.vue.vm" : filename; // 左树右上列表下列表属性
            filename = (string.IsNullOrEmpty(input.LeftTab) && !string.IsNullOrEmpty(input.BottomTab)) ? "web_views_UDIndx.vue.vm" : filename; // 右上列表下列表属性
        }
        //更改list控件
        if (filename == "web_views_List.vue.vm")
        {
            filename = data.TabType switch
            {
                "Tree" => "web_views_TreeList.vue.vm",
                _ => "web_views_List.vue.vm" // 默认列表
            };
        }
        var templateFilePath = Path.Combine(templatePath, filename);
        if (!File.Exists(templateFilePath)) return null;

        var tContent = await File.ReadAllTextAsync(templateFilePath);

        if (template.Type == CodeGenTypeEnum.SeedData)
        {
            // 种子模板
            var seedData = new
            {
                AuthorName = input.AuthorName!, // 作者
                BusName = input.BusName!, // 业务名称
                NameSpace = input.NameSpace!, // 命名空间
                ClassName = input.TableName!, // 类名称
                ConfigId = input.ConfigId, // 库标识
                MenuList = menuList, // 菜单集合
                PrintType = input.PrintType!
            };
            tResult = await _viewEngine.RunCompileAsync(tContent, seedData, builderAction: builder =>
            {
                builder.AddAssemblyReferenceByName("System.Linq");
                builder.AddAssemblyReferenceByName("System.Collections");
                builder.AddAssemblyReferenceByName("System.Text.RegularExpressions");
                builder.AddUsing("System.Text.RegularExpressions");
                builder.AddUsing("System.Collections.Generic");
                builder.AddUsing("System.Linq");
            });
        }
        else
        {
            tResult = await _viewEngine.RunCompileAsync(tContent, data, builderAction: builder =>
            {
                builder.AddAssemblyReferenceByName("System.Linq");
                builder.AddAssemblyReferenceByName("System.Collections");
                builder.AddAssemblyReferenceByName("System.Text.RegularExpressions");
                builder.AddUsing("System.Text.RegularExpressions");
                builder.AddUsing("System.Collections.Generic");
                builder.AddUsing("System.Linq");
            });
        }

        return tResult;
    }

    /// <summary>
    /// 获取连表的实体名和别名
    /// </summary>
    /// <param name="configs"></param>
    /// <returns></returns>
    private static (string, string) GetJoinTableStr(List<CodeGenConfig> configs)
    {
        var uploads = configs.Where(u => u.EffectType == "Upload").ToList();
        var fks = configs.Where(u => u.EffectType == "ForeignKey").ToList();
        string str = ""; // <Order, OrderItem, Custom>
        string lowerStr = ""; // (o, i, c)
        foreach (var item in uploads)
        {
            lowerStr += "sysFile_FK_" + item.LowerPropertyName + ",";
            str += "SysFile,";
        }
        foreach (var item in fks)
        {
            lowerStr += item.LowerFkEntityName + "_FK_" + item.LowerFkColumnName + ",";
            str += item.FkEntityName + ",";
        }
        return (str.TrimEnd(','), lowerStr.TrimEnd(','));
    }

    /// <summary>
    /// 增加菜单
    /// </summary>
    /// <param name="menus"></param>
    /// <param name="pid"></param>
    /// <returns></returns>
    private async Task AddMenu(List<SysMenu> menus, long pid)
    {
        // 若 pid=0 为顶级则创建菜单目录
        if (pid == 0)
        {
            // 若已存在相同目录则删除本级和下级
            var menuType0 = menus.FirstOrDefault(u => u.Type == MenuTypeEnum.Dir && u.Pid == 0);
            var menuList0 = await _db.Queryable<SysMenu>().Where(u => u.Title == menuType0.Title && u.Type == menuType0.Type).ToListAsync();
            if (menuList0.Count > 0)
            {
                var listIds = menuList0.Select(u => u.Id).ToList();
                var childrenIds = new List<long>();
                foreach (var item in listIds)
                {
                    var children = await _db.Queryable<SysMenu>().ToChildListAsync(u => u.Pid, item);
                    childrenIds.AddRange(children.Select(u => u.Id).ToList());
                }
                listIds.AddRange(childrenIds);
                await _db.Deleteable<SysMenu>().Where(u => listIds.Contains(u.Id)).ExecuteCommandAsync();
                await _db.Deleteable<SysRoleMenu>().Where(u => listIds.Contains(u.MenuId)).ExecuteCommandAsync();
            }
        }

        // 若已存在相同菜单则删除本级和下级
        var menuType = menus.FirstOrDefault(u => u.Type == MenuTypeEnum.Menu);
        var menuListCurrent = await _db.Queryable<SysMenu>().Where(u => u.Title == menuType.Title && u.Type == menuType.Type).ToListAsync();
        if (menuListCurrent.Count > 0)
        {
            var listIds = menuListCurrent.Select(u => u.Id).ToList();
            var childListIds = new List<long>();
            foreach (var item in listIds)
            {
                var childList = await _db.Queryable<SysMenu>().ToChildListAsync(u => u.Pid, item);
                childListIds.AddRange(childList.Select(u => u.Id).ToList());
            }
            listIds.AddRange(childListIds);
            await _db.Deleteable<SysMenu>().Where(u => listIds.Contains(u.Id)).ExecuteCommandAsync();
            await _db.Deleteable<SysRoleMenu>().Where(u => listIds.Contains(u.MenuId)).ExecuteCommandAsync();
        }

        await _db.Insertable(menus).ExecuteCommandAsync();

        // 删除角色菜单按钮缓存
        App.GetRequiredService<SysCacheService>().RemoveByPrefixKey(CacheConst.KeyUserApi);
    }

    /// <summary>
    /// 获得菜单列表
    /// </summary>
    /// <param name="className"></param>
    /// <param name="busName"></param>
    /// <param name="pid"></param>
    /// <param name="menuIcon"></param>
    /// <param name="pagePath"></param>
    /// <param name="tableFieldList"></param>
    /// <returns></returns>
    private async Task<List<SysMenu>> GetMenus(string className, string busName, long pid, string menuIcon, string pagePath, List<CodeGenConfig> tableFieldList)
    {
        string pPath;
        // 若 pid=0 为顶级则创建菜单目录
        SysMenu menuType0 = null;
        long tempPid = pid;
        var menuList = new List<SysMenu>();
        var classNameLower = className.ToLower();
        var classNameFirstLower = className.ToFirstLetterLowerCase();
        if (pid == 0)
        {
            // 目录
            menuType0 = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                Pid = 0,
                Title = busName + "管理",
                Type = MenuTypeEnum.Dir,
                Icon = "robot",
                Path = "/" + classNameLower + "Manage",
                Name = classNameFirstLower + "Manage",
                Component = "Layout",
                OrderNo = 100,
                CreateTime = DateTime.Now
            };
            pid = menuType0.Id;
            pPath = menuType0.Path;
        }
        else
        {
            var pMenu = await _db.Queryable<SysMenu>().FirstAsync(u => u.Id == pid) ?? throw Oops.Oh(ErrorCodeEnum.D1505);
            pPath = pMenu.Path;
        }

        // 菜单
        var menuType = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            Pid = pid,
            Title = busName + "管理",
            Name = classNameFirstLower,
            Type = MenuTypeEnum.Menu,
            Icon = menuIcon,
            Path = pPath + "/" + classNameLower,
            Component = "/" + pagePath + "/" + classNameFirstLower + "/index",
            CreateTime = DateTime.Now
        };

        var menuPid = menuType.Id;
        int menuOrder = 100;
        // 按钮-page
        var menuTypePage = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            Pid = menuPid,
            Title = "查询",
            Type = MenuTypeEnum.Btn,
            Permission = classNameFirstLower + "/page",
            OrderNo = menuOrder,
            CreateTime = DateTime.Now
        };
        menuOrder += 10;
        menuList.Add(menuTypePage);

        // 按钮-detail
        var menuTypeDetail = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            Pid = menuPid,
            Title = "详情",
            Type = MenuTypeEnum.Btn,
            Permission = classNameFirstLower + "/detail",
            OrderNo = menuOrder,
            CreateTime = DateTime.Now
        };
        menuOrder += 10;
        menuList.Add(menuTypeDetail);

        // 按钮-add
        var menuTypeAdd = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            Pid = menuPid,
            Title = "增加",
            Type = MenuTypeEnum.Btn,
            Permission = classNameFirstLower + "/add",
            OrderNo = menuOrder,
            CreateTime = DateTime.Now
        };
        menuOrder += 10;
        menuList.Add(menuTypeAdd);

        // 按钮-delete
        var menuTypeDelete = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            Pid = menuPid,
            Title = "删除",
            Type = MenuTypeEnum.Btn,
            Permission = classNameFirstLower + "/delete",
            OrderNo = menuOrder,
            CreateTime = DateTime.Now
        };
        menuOrder += 10;
        menuList.Add(menuTypeDelete);

        // 按钮-update
        var menuTypeUpdate = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            Pid = menuPid,
            Title = "编辑",
            Type = MenuTypeEnum.Btn,
            Permission = classNameFirstLower + "/update",
            OrderNo = menuOrder,
            CreateTime = DateTime.Now
        };
        menuOrder += 10;
        menuList.Add(menuTypeUpdate);

        // 加入ForeignKey、Upload、ApiTreeSelector 等接口的权限
        // 在生成表格时，有些字段只是查询时显示，不需要填写（WhetherAddUpdate），所以这些字段没必要生成相应接口
        var fkTableList = tableFieldList.Where(u => u.EffectType == "ForeignKey" && (u.WhetherAddUpdate == "Y" || u.QueryWhether == "Y")).ToList();
        foreach (var column in fkTableList)
        {
            var menuType1 = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                Pid = menuPid,
                Title = "外键" + column.ColumnName,
                Type = MenuTypeEnum.Btn,
                Permission = classNameFirstLower + "/" + column.FkEntityName + column.ColumnName + "Dropdown",
                OrderNo = menuOrder,
                CreateTime = DateTime.Now
            };
            menuOrder += 10;
            menuList.Add(menuType1);
        }
        var treeSelectTableList = tableFieldList.Where(u => u.EffectType == "ApiTreeSelector").ToList();
        foreach (var column in treeSelectTableList)
        {
            var menuType1 = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                Pid = menuPid,
                Title = "树型" + column.ColumnName,
                Type = MenuTypeEnum.Btn,
                Permission = classNameFirstLower + "/" + column.FkEntityName + "Tree",
                OrderNo = menuOrder,
                CreateTime = DateTime.Now
            };
            menuOrder += 10;
            menuList.Add(menuType1);
        }
        var uploadTableList = tableFieldList.Where(u => u.EffectType == "Upload").ToList();
        foreach (var column in uploadTableList)
        {
            var menuType1 = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                Pid = menuPid,
                Title = "上传" + column.ColumnName,
                Type = MenuTypeEnum.Btn,
                Permission = classNameFirstLower + "/Upload" + column.ColumnName,
                OrderNo = menuOrder,
                CreateTime = DateTime.Now
            };
            menuOrder += 10;
            menuList.Add(menuType1);
        }
        menuList.Insert(0, menuType);
        if (tempPid == 0)
        {
            // 顶级目录需要添加目录本身
            menuList.Insert(0, menuType0);
        }
        return menuList;
    }

    /// <summary>
    /// 获取模板文件集合
    /// </summary>
    /// <returns></returns>
    private List<SysCodeGenTemplate> GetTemplateList(SysCodeGen input)
    {
        //var codeGen= _codeGenRep.AsQueryable()
        //TODO: 只获取选中的模板
        if (input.GenerateType!.Substring(1, 1).Contains('1'))
        {
            return _db.Queryable<SysCodeGenTemplate>()
                .Where(u => u.Type == CodeGenTypeEnum.Frontend)
                .Where(u => u.Id == SqlFunc.Subqueryable<SysCodeGenTemplateRelation>().Where(s => s.CodeGenId == input.Id).GroupBy(s => s.TemplateId).Select(s => s.TemplateId))
                .ToList();
        }

        if (input.GenerateType.Substring(1, 1).Contains('2'))
        {
            return _db.Queryable<SysCodeGenTemplate>()
                .Where(u => u.Type == CodeGenTypeEnum.Backend)
                .Where(u => u.Id == SqlFunc.Subqueryable<SysCodeGenTemplateRelation>().Where(s => s.CodeGenId == input.Id).GroupBy(s => s.TemplateId).Select(s => s.TemplateId))
                .ToList();
        }
        else
        {
            return _db.Queryable<SysCodeGenTemplate>()
                .Where(u => u.Id == SqlFunc.Subqueryable<SysCodeGenTemplateRelation>().Where(s => s.CodeGenId == input.Id).GroupBy(s => s.TemplateId).Select(s => s.TemplateId))
                .ToList();
        }
    }
}