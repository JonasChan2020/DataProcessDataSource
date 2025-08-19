// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统代码生成配置服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 260, Description = "代码生成配置")]
public class SysCodeGenConfigService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly CodeGenOptions _codeGenOptions;
    private readonly DbConnectionOptions _dbConnectionOptions;

    public SysCodeGenConfigService(ISqlSugarClient db,
        IOptions<CodeGenOptions> codeGenOptions,
        IOptions<DbConnectionOptions> dbConnectionOptions)
    {
        _db = db;
        _codeGenOptions = codeGenOptions.Value;
        _dbConnectionOptions = dbConnectionOptions.Value;
    }

    /// <summary>
    /// 获取数据表列（实体属性）集合
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取数据表列（实体属性）集合")]
    public List<ColumnOuput> GetColumnList([FromQuery] AddCodeGenInput input)
    {
        return GetColumnList(input.TableName, input.ConfigId);
    }

    /// <summary>
    /// 获取数据表列（实体属性）集合
    /// </summary>
    /// <param name="EntityName"></param>
    /// <param name="ConfigId"></param>
    /// <returns></returns>
    [DisplayName("获取数据表列（实体属性）集合")]
    public List<ColumnOuput> GetColumnList(string EntityName, string ConfigId)
    {
        var entityType = GetEntityInfos().GetAwaiter().GetResult().FirstOrDefault(u => u.EntityName == EntityName);
        if (entityType == null) return null;

        var config = _dbConnectionOptions.ConnectionConfigs.FirstOrDefault(u => u.ConfigId.ToString() == ConfigId);
        var dbTableName = config!.DbSettings.EnableUnderLine ? UtilMethods.ToUnderLine(entityType.DbTableName) : entityType.DbTableName;
        int bracketIndex = dbTableName.IndexOf('{');
        if (bracketIndex != -1)
        {
            dbTableName = dbTableName[..bracketIndex];
            var dbTableInfos = _db.AsTenant().GetConnectionScope(ConfigId).DbMaintenance.GetTableInfoList(false);
            var table = dbTableInfos.FirstOrDefault(u => u.Name.StartsWith(config.DbSettings.EnableUnderLine ? UtilMethods.ToUnderLine(dbTableName) : dbTableName, StringComparison.CurrentCultureIgnoreCase));
            if (table != null)
                dbTableName = table.Name;
        }

        // 切库---多库代码生成用
        var provider = _db.AsTenant().GetConnectionScope(!string.IsNullOrEmpty(ConfigId) ? ConfigId : SqlSugarConst.MainConfigId);

        var entityBasePropertyNames = _codeGenOptions.EntityBaseColumn[nameof(EntityTenantBaseData)];
        var columnInfos = provider.DbMaintenance.GetColumnInfosByTableName(dbTableName, false);
        var result = columnInfos.Select(u => new ColumnOuput
        {
            // 转下划线后的列名需要再转回来（暂时不转）
            //ColumnName = config.DbSettings.EnableUnderLine ? CodeGenUtil.CamelColumnName(u.DbColumnName, entityBasePropertyNames) : u.DbColumnName,
            ColumnName = u.DbColumnName,
            ColumnLength = u.Length,
            IsPrimarykey = u.IsPrimarykey,
            IsNullable = u.IsNullable,
            ColumnKey = u.IsPrimarykey.ToString(),
            NetType = CodeGenHelper.ConvertDataType(u, provider.CurrentConnectionConfig.DbType),
            DataType = u.DataType,
            ColumnComment = string.IsNullOrWhiteSpace(u.ColumnDescription) ? u.DbColumnName : u.ColumnDescription,
            DefaultValue = u.DefaultValue,
        }).ToList();

        // 获取实体的属性信息，赋值给PropertyName属性(CodeFirst模式应以PropertyName为实际使用名称)
        var entityProperties = entityType.Type.GetProperties();
        for (int i = result.Count - 1; i >= 0; i--)
        {
            var columnOutput = result[i];
            // 先找自定义字段名的，如果找不到就再找自动生成字段名的(并且过滤掉没有SugarColumn的属性)
            var propertyInfo = entityProperties.FirstOrDefault(u => (u.GetCustomAttribute<SugarColumn>()?.ColumnName ?? "").ToLower() == columnOutput.ColumnName.ToLower()) ??
                entityProperties.FirstOrDefault(u => u.GetCustomAttribute<SugarColumn>() != null && u.Name.ToLower() == (config.DbSettings.EnableUnderLine
                ? CodeGenHelper.CamelColumnName(columnOutput.ColumnName, entityBasePropertyNames).ToLower()
                : columnOutput.ColumnName.ToLower()));
            if (propertyInfo != null)
            {
                columnOutput.PropertyName = propertyInfo.Name;
                columnOutput.ColumnComment = propertyInfo.GetCustomAttribute<SugarColumn>()!.ColumnDescription;
            }
            else
            {
                result.RemoveAt(i); // 移除没有定义此属性的字段
            }
        }
        return result;
    }

    /// <summary>
    /// 获取库表信息
    /// </summary>
    /// <param name="excludeSysTable">是否排除带SysTable属性的表</param>
    /// <returns></returns>
    [DisplayName("获取库表信息")]
    public async Task<IEnumerable<EntityInfo>> GetEntityInfos(bool excludeSysTable = false)
    {
        var types = new List<Type>();
        if (_codeGenOptions.EntityAssemblyNames != null)
        {
            types = App.EffectiveTypes.Where(u => u.IsClass).Where(u => _codeGenOptions.EntityAssemblyNames.Contains(u.Assembly.GetName().Name)
                || _codeGenOptions.EntityAssemblyNames.Any(name => u.Assembly.GetName().Name!.Contains(name))).ToList();
        }

        Type[] cosType = types.Where(u => u.IsDefined(typeof(SugarTable), false) && !u.GetCustomAttributes<IgnoreTableAttribute>().Any()).ToArray();
        var entityInfos = new List<EntityInfo>();
        foreach (var ct in cosType)
        {
            // 若实体贴[SysTable]特性，则禁止显示系统自带的
            if (excludeSysTable && ct.IsDefined(typeof(SysTableAttribute), false))
                continue;

            var des = ct.GetCustomAttributes(typeof(DescriptionAttribute), false);
            var description = des.Length > 0 ? ((DescriptionAttribute)des[0]).Description : "";
            var sugarAttribute = ct.GetCustomAttributes(typeof(SugarTable), true).FirstOrDefault();

            entityInfos.Add(new EntityInfo()
            {
                EntityName = ct.Name,
                DbTableName = sugarAttribute == null ? ct.Name : ((SugarTable)sugarAttribute).TableName,
                TableDescription = sugarAttribute == null ? description : ((SugarTable)sugarAttribute).TableDescription,
                Type = ct
            });
        }
        return await Task.FromResult(entityInfos);
    }

    /// <summary>
    /// 获取代码生成配置列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取代码生成配置列表")]
    public async Task<List<CodeGenConfig>> GetList([FromQuery] CodeGenConfig input)
    {
        // 获取主表
        var codeGenTable = _db.Queryable<SysCodeGen>().Single(u => u.Id == input.CodeGenId);
        // 获取配置的字段
        var genConfigColumnList = await _db.Queryable<SysCodeGenConfig>().Where(u => u.CodeGenId == input.CodeGenId).ToListAsync();
        // 获取实体所有字段
        var tableColumnList = GetColumnList(codeGenTable.TableName, codeGenTable.ConfigId);
        // 获取新增的字段
        var addColumnList = tableColumnList.Where(u => !genConfigColumnList.Select(d => d.ColumnName).Contains(u.ColumnName)).ToList();
        // 获取删除的字段
        var delColumnList = genConfigColumnList.Where(u => !tableColumnList.Select(d => d.ColumnName).Contains(u.ColumnName)).ToList();
        // 获取更新的字段
        var updateColumnList = new List<SysCodeGenConfig>();
        foreach (var column in genConfigColumnList)
        {
            // 获取没有增减的
            if (tableColumnList.Any(u => u.ColumnName == column.ColumnName))
            {
                var nmd = tableColumnList.Single(u => u.ColumnName == column.ColumnName);
                // 如果数据库类型或者长度改变
                if (nmd.NetType != column.NetType || nmd.ColumnLength != column.ColumnLength || nmd.ColumnComment != column.ColumnComment)
                {
                    column.NetType = nmd.NetType;
                    column.ColumnLength = nmd.ColumnLength;
                    column.ColumnComment = nmd.ColumnComment;
                    updateColumnList.Add(column);
                }
            }
        }
        // 增加新增的
        if (addColumnList.Count > 0) AddList(addColumnList, codeGenTable);
        // 删除没有的
        if (delColumnList.Count > 0) await _db.Deleteable(delColumnList).ExecuteCommandAsync();
        // 更新配置
        if (updateColumnList.Count > 0) await _db.Updateable(updateColumnList).ExecuteCommandAsync();
        // 重新获取配置
        return await _db.Queryable<SysCodeGenConfig>()
            .Where(u => u.CodeGenId == input.CodeGenId)
            .Select<CodeGenConfig>()
            .Mapper(u =>
            {
                u.NetType = (u.EffectType is "EnumSelector" or "ConstSelector" ? u.DictTypeCode : u.NetType);
            })
            .OrderBy(u => new { u.OrderNo, u.Id })
            .ToListAsync();
    }

    /// <summary>
    /// 更新代码生成配置 🔖
    /// </summary>
    /// <param name="inputList"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新代码生成配置")]
    public async Task UpdateCodeGenConfig(List<CodeGenConfig> inputList)
    {
        if (inputList == null || inputList.Count < 1) return;

        await _db.Updateable(inputList.Adapt<List<SysCodeGenConfig>>())
            .IgnoreColumns(u => new { u.ColumnLength, u.ColumnName, u.PropertyName })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除代码生成配置
    /// </summary>
    /// <param name="codeGenId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task DeleteCodeGenConfig(long codeGenId)
    {
        await _db.Deleteable<SysCodeGenConfig>().Where(u => u.CodeGenId == codeGenId).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取代码生成配置详情 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取代码生成配置详情")]
    public async Task<SysCodeGenConfig> GetDetail([FromQuery] CodeGenConfig input)
    {
        return await _db.Queryable<SysCodeGenConfig>().SingleAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 批量增加代码生成配置
    /// </summary>
    /// <param name="tableColumnOutputList"></param>
    /// <param name="codeGenerate"></param>
    [NonAction]
    public void AddList(List<ColumnOuput> tableColumnOutputList, SysCodeGen codeGenerate)
    {
        if (tableColumnOutputList == null) return;

        var codeGenConfigs = new List<SysCodeGenConfig>();
        var orderNo = 1;
        foreach (var tableColumn in tableColumnOutputList)
        {
            if (_db.Queryable<SysCodeGenConfig>().Any(u => u.ColumnName == tableColumn.ColumnName && u.CodeGenId == codeGenerate.Id))
                continue;

            var codeGenConfig = new SysCodeGenConfig();

            var YesOrNo = YesNoEnum.Y.ToString();
            if (Convert.ToBoolean(tableColumn.ColumnKey))
            {
                YesOrNo = YesNoEnum.N.ToString();
            }

            if (CodeGenHelper.IsCommonColumn(tableColumn.PropertyName))
            {
                codeGenConfig.WhetherCommon = YesNoEnum.Y.ToString();
                YesOrNo = YesNoEnum.N.ToString();
            }
            else
            {
                codeGenConfig.WhetherCommon = YesNoEnum.N.ToString();
            }

            codeGenConfig.CodeGenId = codeGenerate.Id;
            codeGenConfig.ColumnName = tableColumn.ColumnName; // 字段名
            codeGenConfig.PropertyName = tableColumn.PropertyName;// 实体属性名
            codeGenConfig.ColumnLength = tableColumn.ColumnLength;// 长度
            codeGenConfig.ColumnComment = tableColumn.ColumnComment;
            codeGenConfig.NetType = tableColumn.NetType;
            codeGenConfig.WhetherRetract = YesNoEnum.N.ToString();

            // 生成代码时，主键并不是必要输入项，故一定要排除主键字段
            //codeGenConfig.WhetherRequired = (tableColumn.IsNullable || tableColumn.IsPrimarykey) ? YesNoEnum.N.ToString() : YesNoEnum.Y.ToString();

            #region 添加校验规则

            // 添加校验规则
            codeGenConfig.Id = YitIdHelper.NextId();
            // 验证规则
            List<VerifyRuleItem> ruleItems = [];
            if (!tableColumn.IsNullable && !tableColumn.IsPrimarykey)
            {
                ruleItems.Add(new VerifyRuleItem()
                {
                    Key = codeGenConfig.Id,
                    Type = "required",
                    Message = $"{tableColumn.ColumnComment}不能为空",
                });
            }
            codeGenConfig.WhetherRequired = ruleItems.Any(t => t.Type == "required") ? YesNoEnum.Y.ToString() : YesNoEnum.N.ToString();
            codeGenConfig.Rules = ruleItems.ToJson();

            #endregion 添加校验规则

            codeGenConfig.QueryWhether = YesNoEnum.N.ToString();
            codeGenConfig.WhetherAddUpdate = YesOrNo;
            codeGenConfig.WhetherTable = YesOrNo;

            codeGenConfig.ColumnKey = tableColumn.ColumnKey;

            codeGenConfig.DataType = tableColumn.DataType;
            codeGenConfig.EffectType = CodeGenHelper.DataTypeToEff(codeGenConfig.NetType);
            codeGenConfig.QueryType = GetDefaultQueryType(codeGenConfig); // QueryTypeEnum.eq.ToString();
            codeGenConfig.OrderNo = orderNo;
            codeGenConfig.DefaultValue = GetDefaultValue(tableColumn.DefaultValue);
            codeGenConfigs.Add(codeGenConfig);

            orderNo += 1; // 每个配置排序间隔1
        }
        // 多库代码生成---这里要切回主库
        var provider = _db.AsTenant().GetConnectionScope(SqlSugarConst.MainConfigId);
        provider.Insertable(codeGenConfigs).ExecuteCommand();
    }

    /// <summary>
    /// 默认查询类型
    /// </summary>
    /// <param name="codeGenConfig"></param>
    /// <returns></returns>
    private static string GetDefaultQueryType(SysCodeGenConfig codeGenConfig)
    {
        return (codeGenConfig.NetType?.TrimEnd('?')) switch
        {
            "string" => "like",
            "DateTime" => "~",
            _ => "==",
        };
    }

    /// <summary>
    /// 获取默认值
    /// </summary>
    /// <param name="dataValue"></param>
    /// <returns></returns>
    private static string? GetDefaultValue(string dataValue)
    {
        if (dataValue == null) return null;
        // 正则表达式模式
        // \( 和 \) 用来匹配字面量的括号
        // .+ 用来匹配一个或多个任意字符，但不包括换行符
        string pattern = @"\((.+)\)";//适合MSSQL其他数据库没有测试

        // 使用 Regex 类进行匹配
        Match match = Regex.Match(dataValue, pattern);

        string value;
        // 如果找到了匹配项
        if (match.Success)
        {
            // 提取括号内的值
            value = match.Groups[1].Value.Trim('\'');
        }
        else
        {
            value = dataValue;
        }
        return value;
    }
}