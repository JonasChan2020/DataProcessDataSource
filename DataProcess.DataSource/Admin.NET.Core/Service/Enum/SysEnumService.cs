// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统枚举服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 275, Description = "系统枚举")]
public class SysEnumService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly EnumOptions _enumOptions;
    private const int OrderOffset = 10;
    private const string DefaultTagType = "info";

    public SysEnumService(ISqlSugarClient db, IOptions<EnumOptions> enumOptions)
    {
        _db = db;
        _enumOptions = enumOptions.Value;
    }

    /// <summary>
    /// 获取所有枚举类型 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取所有枚举类型")]
    public List<EnumTypeOutput> GetEnumTypeList()
    {
        var enumTypeList = App.EffectiveTypes.Where(t => t.IsEnum)
            .Where(t => _enumOptions.EntityAssemblyNames.Contains(t.Assembly.GetName().Name) ||
                        _enumOptions.EntityAssemblyNames.Any(name => t.Assembly.GetName().Name.Contains(name)))
            .Where(t => t.GetCustomAttributes(typeof(ErrorCodeTypeAttribute), false).Length == 0) // 排除错误代码类型
            .OrderBy(u => u.Name).ThenBy(u => u.FullName)
            .ToList();

        return enumTypeList.Select(GetEnumDescription).ToList();
    }

    /// <summary>
    /// 获取字典描述
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static EnumTypeOutput GetEnumDescription(Type type)
    {
        string description = type.Name;
        var attrs = type.GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attrs.Length != 0)
        {
            var att = ((DescriptionAttribute[])attrs)[0];
            description = att.Description;
        }

        var enumType = App.EffectiveTypes.FirstOrDefault(t => t.IsEnum && t.Name == type.Name);
        return new EnumTypeOutput
        {
            TypeDescribe = description,
            TypeName = type.Name,
            TypeRemark = description,
            EnumEntities = (enumType ?? type).EnumToList()
        };
    }

    /// <summary>
    /// 通过枚举类型获取枚举值集合 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("通过枚举类型获取枚举值集合")]
    public List<EnumEntity> GetEnumDataList([FromQuery] EnumInput input)
    {
        var enumType = App.EffectiveTypes.FirstOrDefault(u => u.IsEnum && u.Name == input.EnumName);
        if (enumType is not { IsEnum: true })
            throw Oops.Oh(ErrorCodeEnum.D1503);

        return enumType.EnumToList();
    }

    /// <summary>
    /// 通过实体的字段名获取相关枚举值集合（目前仅支持枚举类型） 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("通过实体的字段名获取相关枚举值集合")]
    public static List<EnumEntity> GetEnumDataListByField([FromQuery] QueryEnumDataInput input)
    {
        // 获取实体类型属性
        Type entityType = App.EffectiveTypes.FirstOrDefault(u => u.Name == input.EntityName) ?? throw Oops.Oh(ErrorCodeEnum.D1504);

        // 获取字段类型
        var fieldType = entityType.GetProperties().FirstOrDefault(u => u.Name == input.FieldName)?.PropertyType;
        if (fieldType is not { IsEnum: true })
            throw Oops.Oh(ErrorCodeEnum.D1503);

        return fieldType.EnumToList();
    }

    /// <summary>
    /// 枚举转字典
    /// </summary>
    /// <returns></returns>
    [DisplayName("枚举转字典")]
    [UnitOfWork]
    public async Task EnumToDict()
    {
        // 获取枚举类型列表
        var enumTypeList = GetEnumTypeList();
        var enumCodeList = enumTypeList.Select(u => u.TypeName);
        // 查询数据库中已存在的枚举类型代码
        //var exp = Expressionable.Create<SysDictType, SingleColumnEntity<string>>().And((t1, t2) => t1.Code == t2.ColumnName).ToExpression();
        //var sysDictTypeList = await db.Queryable<SysDictType>().Includes(t1 => t1.Children).BulkListQuery(exp, enumCodeList, stoppingToken);
        var sysDictTypeList = await _db.Queryable<SysDictType>().Includes(u => u.Children)
            .Where(u => enumCodeList.Contains(u.Code)).ToListAsync();
        // 更新的枚举转换字典
        var updatedEnumCodes = sysDictTypeList.Select(u => u.Code);
        var updatedEnumType = enumTypeList.Where(u => updatedEnumCodes.Contains(u.TypeName)).ToList();
        var sysDictTypeDict = sysDictTypeList.ToDictionary(u => u.Code, u => u);
        var (updatedDictTypes, updatedDictDatas, newSysDictDatas) = GetUpdatedDicts(updatedEnumType, sysDictTypeDict);

        // 新增的枚举转换字典
        var newEnumType = enumTypeList.Where(u => !updatedEnumCodes.Contains(u.TypeName)).ToList();
        var (newDictTypes, newDictDatas) = GetNewSysDicts(newEnumType);

        // 若是Sqlite、PostgreSQL、SqlServer、MySql、GaussDB、Kdbndp则采用批量处理
        bool enableBulk = _db.CurrentConnectionConfig.DbType is SqlSugar.DbType.Sqlite or SqlSugar.DbType.PostgreSQL
            or SqlSugar.DbType.SqlServer or SqlSugar.DbType.MySql;
        // or SqlSugar.DbType.MySqlConnector
        // or SqlSugar.DbType.GaussDB
        // or SqlSugar.DbType.Kdbndp;

        // 执行数据库操作
        if (updatedDictTypes.Count > 0)
        {
            if (enableBulk) await _db.Fastest<SysDictType>().PageSize(300).BulkMergeAsync(updatedDictTypes);
            else await _db.Updateable(updatedDictTypes).ExecuteCommandAsync();
        }

        if (updatedDictDatas.Count > 0)
        {
            if (enableBulk) await _db.Fastest<SysDictData>().PageSize(300).BulkMergeAsync(updatedDictDatas);
            else await _db.Updateable(updatedDictDatas).ExecuteCommandAsync();
        }

        if (newSysDictDatas.Count > 0)
        {
            if (enableBulk) await _db.Fastest<SysDictData>().PageSize(300).BulkMergeAsync(newSysDictDatas);
            else
            {
                // 达梦：用db.Insertable(newSysDictDatas).ExecuteCommandAsync(stoppingToken)；插入400条以上会内容溢出错误，所以改用逐条插入
                // 达梦：不支持storageable2.BulkUpdateAsync 功能
                foreach (var dd in newSysDictDatas)
                    await _db.Insertable(dd).ExecuteCommandAsync();
            }
        }

        if (newDictTypes.Count > 0)
        {
            if (enableBulk) await _db.Fastest<SysDictType>().PageSize(300).BulkMergeAsync(newDictTypes);
            else await _db.Insertable(newDictTypes).ExecuteCommandAsync();
        }

        if (newDictDatas.Count > 0)
        {
            if (enableBulk) await _db.Fastest<SysDictData>().PageSize(300).BulkMergeAsync(newDictDatas);
            else
            {
                // 达梦：用db.Insertable(newDictDatas).ExecuteCommandAsync(stoppingToken)；插入400条以上会内容溢出错误，所以改用逐条插入
                // 达梦：不支持storageable2.BulkUpdateAsync 功能
                foreach (var dd in newDictDatas)
                    await _db.Insertable(dd).ExecuteCommandAsync();
            }
        }
    }

    /// <summary>
    /// 获取需要新增的字典列表
    /// </summary>
    /// <param name="addEnumType"></param>
    /// <returns>
    /// 一个元组，包含以下元素：
    /// <list type="table">
    ///     <item><term>SysDictTypes</term><description>字典类型列表</description></item>
    ///     <item><term>SysDictDatas</term><description>字典数据列表</description></item>
    /// </list>
    /// </returns>
    private (List<SysDictType>, List<SysDictData>) GetNewSysDicts(List<EnumTypeOutput> addEnumType)
    {
        var newDictType = new List<SysDictType>();
        var newDictData = new List<SysDictData>();
        if (addEnumType.Count <= 0)
            return (newDictType, newDictData);

        // 新增字典类型
        newDictType = addEnumType.Select(u => new SysDictType
        {
            Id = YitIdHelper.NextId(),
            Code = u.TypeName,
            Name = u.TypeDescribe,
            Remark = u.TypeRemark,
            Status = StatusEnum.Enable,
            SysFlag = YesNoEnum.Y,
        }).ToList();

        // 新增字典数据
        newDictData = addEnumType.Join(newDictType, t1 => t1.TypeName, t2 => t2.Code, (t1, t2) => new
        {
            Data = t1.EnumEntities.Select(u => new SysDictData
            {
                Id = YitIdHelper.NextId(),
                DictTypeId = t2.Id,
                Code = u.Name,
                Label = u.Describe,
                Value = u.Value.ToString(),
                Remark = t2.Remark,
                OrderNo = u.Value + OrderOffset,
                TagType = u.Theme != "" ? u.Theme : DefaultTagType,
            }).ToList()
        }).SelectMany(x => x.Data).ToList();

        return (newDictType, newDictData);
    }

    /// <summary>
    /// 获取需要更新的字典列表
    /// </summary>
    /// <param name="updatedEnumType"></param>
    /// <param name="sysDictTypeDict"></param>
    /// <returns>
    /// 一个元组，包含以下元素：
    /// <list type="table">
    ///     <item><term>SysDictTypes</term><description>更新字典类型列表</description>
    ///     </item>
    ///     <item><term>SysDictDatas</term><description>更新字典数据列表</description>
    ///     </item>
    ///     <item><term>SysDictDatas</term><description>新增字典数据列表</description>
    ///     </item>
    /// </list>
    /// </returns>
    private (List<SysDictType>, List<SysDictData>, List<SysDictData>) GetUpdatedDicts(List<EnumTypeOutput> updatedEnumType, Dictionary<string, SysDictType> sysDictTypeDict)
    {
        var updatedSysDictTypes = new List<SysDictType>();
        var updatedSysDictData = new List<SysDictData>();
        var newSysDictData = new List<SysDictData>();
        foreach (var e in updatedEnumType)
        {
            if (!sysDictTypeDict.TryGetValue(e.TypeName, out var value))
                continue;

            var updatedDictType = value;
            updatedDictType.Name = e.TypeDescribe;
            updatedDictType.Remark = e.TypeRemark;
            updatedSysDictTypes.Add(updatedDictType);
            var updatedDictData = updatedDictType.Children.Where(u => u.DictTypeId == updatedDictType.Id).ToList();

            // 遍历需要更新的字典数据
            foreach (var dictData in updatedDictData)
            {
                var enumData = e.EnumEntities.FirstOrDefault(u => dictData.Code == u.Name);
                if (enumData != null)
                {
                    dictData.Code = enumData.Name;
                    dictData.Label = enumData.Describe;
                    dictData.Value = enumData.Value.ToString();
                    dictData.OrderNo = enumData.Value + OrderOffset;
                    dictData.TagType = enumData.Theme != "" ? enumData.Theme : dictData.TagType != "" ? dictData.TagType : DefaultTagType;
                    updatedSysDictData.Add(dictData);
                }
            }

            // 新增的枚举值名称列表
            var newEnumDataNameList = e.EnumEntities.Select(u => u.Name).Except(updatedDictData.Select(u => u.Code));
            foreach (var newEnumDataName in newEnumDataNameList)
            {
                var enumData = e.EnumEntities.FirstOrDefault(u => newEnumDataName == u.Name);
                if (enumData != null)
                {
                    var dictData = new SysDictData
                    {
                        Id = YitIdHelper.NextId(),
                        DictTypeId = updatedDictType.Id,
                        Code = enumData.Name,
                        Label = enumData.Describe,
                        Value = enumData.Value.ToString(),
                        Remark = updatedDictType.Remark,
                        OrderNo = enumData.Value + OrderOffset,
                        TagType = enumData.Theme != "" ? enumData.Theme : DefaultTagType,
                    };
                    dictData.TagType = enumData.Theme != "" ? enumData.Theme : dictData.TagType != "" ? dictData.TagType : DefaultTagType;
                    newSysDictData.Add(dictData);
                }
            }

            // 删除的情况暂不处理
        }

        return (updatedSysDictTypes, updatedSysDictData, newSysDictData);
    }
}