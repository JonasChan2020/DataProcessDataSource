// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.Text.Json;

namespace Admin.NET.Core;

/// <summary>
/// Sqlsugar 扩展方法
/// </summary>
public static class SqlSugarExtension
{
    #region 切换数据库

    /// <summary>
    /// 切换数据库
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="db"></param>
    /// <returns></returns>
    public static ISqlSugarClient ForTenant<TEntity>(this ISqlSugarClient db)
    {
        var attr = typeof(TEntity).GetCustomAttribute<TenantAttribute>();
        var tenantId = attr != null ? GetConfigIdFromAttribute(attr) : SqlSugarConst.DefaultTenantId;
        return db.AsTenant().GetConnectionScope(tenantId ?? SqlSugarConst.DefaultTenantId);
    }

    private static object GetConfigIdFromAttribute(TenantAttribute attr)
    {
        const string targetKey = "configId";
        var type = attr.GetType();

        // 方式1：尝试通过属性获取
        var prop = type.GetProperty(targetKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null) return prop.GetValue(attr);

        // 方式2：尝试通过私有字段获取
        var fieldNames = new[]
        {
            $"<{targetKey}>k__BackingField",  // 自动属性字段
            $"_{targetKey}",                  // 常规私有字段 (如_configId)
            $"m_{targetKey}",                 // 匈牙利命名法
            "configId"                        // 直接字段访问
        };

        foreach (var name in fieldNames)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) return field.GetValue(attr);
        }

        // 方式3：通过Dynamic访问（备用方案）
        try
        {
            dynamic d = attr;
            return d.configId;
        }
        catch
        {
            return null;
        }
    }

    #endregion 切换数据库

    #region 动态查询扩展方法

    /// <summary>
    /// Sqlsugar 动态查询扩展方法
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static ISugarQueryable<T> SearchBy<T>(this ISugarQueryable<T> queryable, BaseFilter filter)
    {
        return queryable.SearchByKeyword(filter.Keyword).AdvancedSearch(filter.Search).AdvancedFilter(filter.Filter);
    }

    public static ISugarQueryable<T> SearchByKeyword<T>(this ISugarQueryable<T> queryable, string keyword)
    {
        return queryable.AdvancedSearch(new Search { Keyword = keyword });
    }

    public static ISugarQueryable<T> AdvancedSearch<T>(this ISugarQueryable<T> queryable, Search search)
    {
        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var paramExpr = Expression.Parameter(typeof(T));

            Expression right = Expression.Constant(false);

            if (search.Fields?.Count != 0)
            {
                foreach (string field in search.Fields)
                {
                    MemberExpression propertyExpr = GetPropertyExpression(field, paramExpr);
                    var left = AddSearchPropertyByKeyword<T>(propertyExpr, search.Keyword);
                    right = Expression.Or(left, right);
                }
            }
            else
            {
                var properties = typeof(T).GetProperties().Where(prop => Nullable.GetUnderlyingType(prop.PropertyType) == null
                    && !prop.PropertyType.IsEnum
                    && Type.GetTypeCode(prop.PropertyType) != TypeCode.Object);

                foreach (var property in properties)
                {
                    var propertyExpr = Expression.Property(paramExpr, property);
                    var left = AddSearchPropertyByKeyword<T>(propertyExpr, search.Keyword);
                    right = Expression.Or(left, right);
                }
            }

            var lambda = Expression.Lambda<Func<T, bool>>(right, paramExpr);
            return queryable.Where(lambda);
        }

        return queryable;
    }

    public static ISugarQueryable<T> AdvancedFilter<T>(this ISugarQueryable<T> queryable, Filter filter)
    {
        if (filter is not null)
        {
            var parameter = Expression.Parameter(typeof(T));

            Expression binaryExpresioFilter;

            if (Enum.IsDefined(typeof(FilterLogicEnum), filter.Logic))
            {
                if (filter.Filters is null) throw new ArgumentException("The Filters attribute is required when declaring a logic");
                binaryExpresioFilter = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
            }
            else
            {
                var filterValid = GetValidFilter(filter);
                binaryExpresioFilter = CreateFilterExpression(filterValid.Field!, filterValid.Operator, filterValid.Value, parameter);
            }

            var lambda = Expression.Lambda<Func<T, bool>>(binaryExpresioFilter, parameter);

            return queryable.Where(lambda);
        }
        return queryable;
    }

    private static Expression CombineFilter(FilterLogicEnum filterLogic, Expression bExpresionBase, Expression bExpresion)
    {
        return filterLogic switch
        {
            FilterLogicEnum.And => Expression.And(bExpresionBase, bExpresion),
            FilterLogicEnum.Or => Expression.Or(bExpresionBase, bExpresion),
            FilterLogicEnum.Xor => Expression.ExclusiveOr(bExpresionBase, bExpresion),
            _ => throw new ArgumentException("FilterLogic is not valid.", nameof(filterLogic)),
        };
    }

    private static Filter GetValidFilter(Filter filter)
    {
        if (string.IsNullOrEmpty(filter.Field)) throw new ArgumentException("The field attribute is required when declaring a filter");
        if (filter.Operator.IsNullOrEmpty()) throw new ArgumentException("The Operator attribute is required when declaring a filter");
        return filter;
    }

    private static Expression CreateFilterExpression(FilterLogicEnum filterLogic, IEnumerable<Filter> filters, ParameterExpression parameter)
    {
        Expression filterExpression = default!;

        foreach (var filter in filters)
        {
            Expression bExpresionFilter;

            if (Enum.IsDefined(typeof(FilterLogicEnum), filter.Logic))
            {
                if (filter.Filters is null) throw new ArgumentException("The Filters attribute is required when declaring a logic");
                bExpresionFilter = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
            }
            else
            {
                var filterValid = GetValidFilter(filter);
                bExpresionFilter = CreateFilterExpression(filterValid.Field!, filterValid.Operator, filterValid.Value, parameter);
            }

            filterExpression = filterExpression is null ? bExpresionFilter : CombineFilter(filterLogic, filterExpression, bExpresionFilter);
        }

        return filterExpression;
    }

    private static Expression CreateFilterExpression(string field, FilterOperatorEnum filterOperator, object? value, ParameterExpression parameter)
    {
        var propertyExpresion = GetPropertyExpression(field, parameter);
        var valueExpresion = GeValuetExpression(field, value, propertyExpresion.Type);
        return CreateFilterExpression(propertyExpresion, valueExpresion, filterOperator);
    }

    private static Expression CreateFilterExpression(MemberExpression memberExpression, ConstantExpression constantExpression, FilterOperatorEnum filterOperator)
    {
        return filterOperator switch
        {
            FilterOperatorEnum.EQ => Expression.Equal(memberExpression, constantExpression),
            FilterOperatorEnum.NEQ => Expression.NotEqual(memberExpression, constantExpression),
            FilterOperatorEnum.LT => Expression.LessThan(memberExpression, constantExpression),
            FilterOperatorEnum.LTE => Expression.LessThanOrEqual(memberExpression, constantExpression),
            FilterOperatorEnum.GT => Expression.GreaterThan(memberExpression, constantExpression),
            FilterOperatorEnum.GTE => Expression.GreaterThanOrEqual(memberExpression, constantExpression),
            FilterOperatorEnum.Contains => Expression.Call(memberExpression, nameof(FilterOperatorEnum.Contains), null, constantExpression),
            FilterOperatorEnum.StartsWith => Expression.Call(memberExpression, nameof(FilterOperatorEnum.StartsWith), null, constantExpression),
            FilterOperatorEnum.EndsWith => Expression.Call(memberExpression, nameof(FilterOperatorEnum.EndsWith), null, constantExpression),
            _ => throw new ArgumentException("Filter Operator is not valid."),
        };
    }

    private static string GetStringFromJsonElement(object value)
    {
        if (value is JsonElement element) return element.GetString()!;
        if (value is string v) return v;
        return value?.ToString();
    }

    private static ConstantExpression GeValuetExpression(string field, object? value, Type propertyType)
    {
        if (value == null) return Expression.Constant(null, propertyType);

        if (propertyType.IsEnum)
        {
            string? stringEnum = GetStringFromJsonElement(value);

            if (!Enum.TryParse(propertyType, stringEnum, true, out object? valueparsed)) throw new ArgumentException(string.Format("Value {0} is not valid for {1}", value, field));

            return Expression.Constant(valueparsed, propertyType);
        }
        if (propertyType == typeof(long) || propertyType == typeof(long?))
        {
            string? stringLong = GetStringFromJsonElement(value);

            if (!long.TryParse(stringLong, out long valueparsed)) throw new ArgumentException(string.Format("Value {0} is not valid for {1}", value, field));

            return Expression.Constant(valueparsed, propertyType);
        }

        if (propertyType == typeof(Guid))
        {
            string? stringGuid = GetStringFromJsonElement(value);

            if (!Guid.TryParse(stringGuid, out Guid valueparsed)) throw new ArgumentException(string.Format("Value {0} is not valid for {1}", value, field));

            return Expression.Constant(valueparsed, propertyType);
        }

        if (propertyType == typeof(string))
        {
            string? text = GetStringFromJsonElement(value);

            return Expression.Constant(text, propertyType);
        }

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            string? text = GetStringFromJsonElement(value);
            return Expression.Constant(ChangeType(text, propertyType), propertyType);
        }

        return Expression.Constant(ChangeType(((JsonElement)value).GetRawText(), propertyType), propertyType);
    }

    private static dynamic? ChangeType(object value, Type conversion)
    {
        var t = conversion;
        if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
                return null;

            t = Nullable.GetUnderlyingType(t);
        }

        return Convert.ChangeType(value, t!);
    }

    private static MemberExpression GetPropertyExpression(string propertyName, ParameterExpression parameter)
    {
        Expression propertyExpression = parameter;
        foreach (string member in propertyName.Split('.'))
        {
            propertyExpression = Expression.PropertyOrField(propertyExpression, member);
        }

        return (MemberExpression)propertyExpression;
    }

    private static Expression AddSearchPropertyByKeyword<T>(Expression propertyExpr, string keyword, FilterOperatorEnum operatorSearch = FilterOperatorEnum.Contains)
    {
        if (propertyExpr is not MemberExpression memberExpr || memberExpr.Member is not PropertyInfo property)
            throw new ArgumentException("propertyExpr must be a property expression.", nameof(propertyExpr));

        ConstantExpression constant = Expression.Constant(keyword);

        MethodInfo method = operatorSearch switch
        {
            FilterOperatorEnum.Contains => typeof(string).GetMethod(nameof(FilterOperatorEnum.Contains), [typeof(string)]),
            FilterOperatorEnum.StartsWith => typeof(string).GetMethod(nameof(FilterOperatorEnum.StartsWith), [typeof(string)]),
            FilterOperatorEnum.EndsWith => typeof(string).GetMethod(nameof(FilterOperatorEnum.EndsWith), [typeof(string)]),
            _ => throw new ArgumentException("Filter Operator is not valid."),
        };

        Expression selectorExpr = property.PropertyType == typeof(string)
            ? propertyExpr
            : Expression.Condition(Expression.Equal(Expression.Convert(propertyExpr, typeof(object)), Expression.Constant(null, typeof(object))), Expression.Constant(null, typeof(string)), Expression.Call(propertyExpr, "ToString", null, null));

        return Expression.Call(selectorExpr, method, constant);
    }

    #endregion 动态查询扩展方法

    #region 初始化库表结构和种子数据

    /// <summary>
    /// 初始化表实体
    /// </summary>
    /// <param name="dbProvider"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static void InitTable<T>(this SqlSugarScopeProvider dbProvider) where T : class, new()
    {
        InitTable(dbProvider, typeof(T));
    }

    /// <summary>
    /// 初始化表实体
    /// </summary>
    /// <param name="entityType"></param>
    /// <param name="dbProvider"></param>
    /// <returns></returns>
    public static void InitTable(this SqlSugarScopeProvider dbProvider, Type entityType)
    {
        // 初始化表实体，如果存在分表特性，需要额外处理
        if (entityType.GetCustomAttribute<SplitTableAttribute>() == null)
            dbProvider.CodeFirst.InitTables(entityType);
        else
            dbProvider.CodeFirst.SplitTables().InitTables(entityType);

        // 将不存在实体中的字段改为可空
        var entityInfo = dbProvider.EntityMaintenance.GetEntityInfo(entityType);
        var dbColumnInfos = dbProvider.DbMaintenance.GetColumnInfosByTableName(entityInfo.DbTableName) ?? [];
        foreach (var dbColumnInfo in dbColumnInfos.Where(dbColumnInfo => !dbColumnInfo.IsPrimarykey && entityInfo.Columns.All(u => u.DbColumnName == null || u.DbColumnName.ToLower() != dbColumnInfo.DbColumnName.ToLower())))
        {
            dbColumnInfo.IsNullable = true;
            dbProvider.DbMaintenance.UpdateColumn(entityInfo.DbTableName, dbColumnInfo);
        }
    }

    /// <summary>
    /// 初始化表种子数据
    /// </summary>
    /// <param name="db"></param>
    /// <param name="handleBefore"></param>
    /// <returns></returns>
    public static (int, int, int)? InitTableSeedData<T>(this SqlSugarScopeProvider db, Action<object> handleBefore = null)
    {
        return InitTableSeedData(db, typeof(T), handleBefore);
    }

    /// <summary>
    /// 初始化表种子数据
    /// </summary>
    /// <param name="dbProvider"></param>
    /// <param name="seedType"></param>
    /// <param name="handleBefore"></param>
    /// <returns></returns>
    public static (int, int, int)? InitTableSeedData(this SqlSugarScopeProvider dbProvider, Type seedType, Action<object> handleBefore = null)
    {
        var config = dbProvider.CurrentConnectionConfig;

        // 获取表实体类型
        var entityType = seedType.GetInterfaces().First().GetGenericArguments().First();

        if (config.ConfigId.ToString() == SqlSugarConst.MainConfigId) // 默认库（有系统表特性、没有日志表和租户表特性）
        {
            if (entityType.GetCustomAttribute<SysTableAttribute>() == null &&
                (entityType.GetCustomAttribute<LogTableAttribute>() != null ||
                 entityType.GetCustomAttribute<TenantAttribute>() != null))
            {
                Console.WriteLine($"    忽略 {seedType.FullName,-58} ({dbProvider.CurrentConnectionConfig.ConfigId}) 原因:非SysTable 与 (LogTable 或 Tenant)");
                return default;
            }
        }
        else if (config.ConfigId.ToString() == SqlSugarConst.LogConfigId) // 日志库
        {
            if (entityType.GetCustomAttribute<LogTableAttribute>() == null)
            {
                Console.WriteLine($"    忽略 {seedType.FullName,-58} ({dbProvider.CurrentConnectionConfig.ConfigId}) 原因:LogTable");
                return default;
            }
        }
        else
        {
            var att = entityType.GetCustomAttribute<TenantAttribute>(); // 自定义的库
            if (att == null || att.configId.ToString() != config.ConfigId.ToString())
            {
                Console.WriteLine($"    忽略 {seedType.FullName,-58} ({dbProvider.CurrentConnectionConfig.ConfigId}) 原因: Tenant 表，但不是这个库：{att.configId} != {config.ConfigId} ");
                return default;
            }
        }

        var instance = Activator.CreateInstance(seedType);
        var hasDataMethod = seedType.GetMethod("HasData");
        var seedData = ((IEnumerable)hasDataMethod?.Invoke(instance, null))?.Cast<object>().ToArray() ?? [];
        if (seedData.Length == 0)
        {
            Console.WriteLine($"    忽略 {seedType.FullName,-58} ({dbProvider.CurrentConnectionConfig.ConfigId}) 原因:没有数据");
            return default;
        }

        // 若实体包含Id字段，则设置为当前租户Id递增1
        var idProperty = entityType.GetProperty(nameof(EntityBaseId.Id));
        var entityInfo = dbProvider.EntityMaintenance.GetEntityInfo(entityType);
        if (idProperty != null && idProperty.PropertyType == typeof(long) && entityInfo.Columns.Any(u => u.PropertyName == nameof(EntityBaseId.Id)))
        {
            var seedId = config.ConfigId.ToLong();
            foreach (var sd in seedData)
            {
                var id = idProperty!.GetValue(sd, null);
                if (id == null || id.ToString() == "0" || string.IsNullOrWhiteSpace(id.ToString()))
                    idProperty.SetValue(sd, ++seedId);
            }
        }

        // 执行前处理种子数据
        if (handleBefore != null) foreach (var sd in seedData) handleBefore(sd);

        int total, insertCount = 0, updateCount = 0;
        if (entityType.GetCustomAttribute<SplitTableAttribute>(true) != null)
        {
            // 拆分表的操作需要实体类型，而通过反射很难实现
            // 所以，这里将Init方法写在“种子数据类”内部，再传入 db 反射调用
            var hasInitMethod = seedType.GetMethod("Init");
            var parameters = new object[] { dbProvider };
            var result = hasInitMethod?.Invoke(instance, parameters) as (int, int, int)?;
            total = result?.Item1 ?? 0;
            insertCount = result?.Item2 ?? 0;
            updateCount = result?.Item3 ?? 0;
        }
        else
        {
            var seedDataList = seedData.ToList();
            total = seedDataList.Count;

            // 按主键进行批量增加和更新
            if (entityInfo.Columns.Any(u => u.IsPrimarykey))
            {
                // 先修改再插入，否则会更新修改时间字段
                var storage = dbProvider.StorageableByObject(seedDataList).ToStorage();
                if (seedType.GetCustomAttribute<IgnoreUpdateSeedAttribute>() == null) // 有忽略更新种子特性时则不更新
                {
                    updateCount = storage.AsUpdateable.IgnoreColumns(entityInfo.Columns.Where(u => u.PropertyInfo.GetCustomAttribute<IgnoreUpdateSeedColumnAttribute>() != null)
                        .Select(u => u.PropertyName).ToArray()).ExecuteCommand();
                }
                insertCount = storage.AsInsertable.ExecuteCommand();
            }
            // 无主键则只进行插入
            else
            {
                if (!dbProvider.Queryable(entityInfo.DbTableName, entityInfo.DbTableName).Any())
                {
                    insertCount = seedDataList.Count;
                    dbProvider.InsertableByObject(seedDataList).ExecuteCommand();
                }
            }
        }
        return (total, insertCount, updateCount);
    }

    #endregion 初始化库表结构和种子数据

    #region 视图操作

    /// <summary>
    /// 获取映射SQL语句, 用于创建视图
    /// </summary>
    /// <param name="queryable"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string ToMappedSqlString<T>(this ISugarQueryable<T> queryable) where T : class
    {
        ArgumentNullException.ThrowIfNull(queryable);

        // 获取实体映射信息
        var entityInfo = queryable.Context.EntityMaintenance.GetEntityInfo(typeof(T));
        if (entityInfo?.Columns == null || entityInfo.Columns.Count == 0) return queryable.ToSqlString();

        // 构建需要替换的字段名映射（只处理实际有差异的字段）
        var nameMap = entityInfo.Columns
            .Where(c => !string.Equals(c.PropertyName, c.DbColumnName, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(k => k.PropertyName.ToLower(), v => v.DbColumnName, StringComparer.OrdinalIgnoreCase);
        if (nameMap.Count == 0) return queryable.ToSqlString();

        // 预编译正则表达式提升性能
        var sql = queryable.ToSqlString();
        foreach (var kv in nameMap)
        {
            sql = Regex.Replace(sql, $@"\b{kv.Key}\b", kv.Value ?? kv.Key, RegexOptions.IgnoreCase | RegexOptions.Compiled); // 单词边界匹配
        }
        return sql;
    }

    #endregion 视图操作
}