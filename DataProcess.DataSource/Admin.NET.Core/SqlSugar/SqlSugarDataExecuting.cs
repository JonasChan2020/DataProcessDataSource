// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 自定义数据审计
/// </summary>
public class SqlSugarDataExecuting
{
    /// <summary>
    /// 缓存数据过滤（内存缓存）
    /// </summary>
    private static readonly ICache _cache = Cache.Default;

    public static void Execute(object oldData, DataFilterModel model)
    {
        var cacheKey = "db:SqlSugarDataExecuting";
        var items = _cache.Get<List<IDataExecuting>>(cacheKey);
        if (items == null)
        {
            items = [];

            // 获取自定义实体过滤器
            var ides = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass && u.GetInterfaces().Any(i => i.HasImplementedRawGeneric(typeof(IDataExecuting))));
            foreach (var ide in ides)
            {
                var instance = Activator.CreateInstance(ide);
                items.Add((IDataExecuting)instance);
            }

            _cache.Add(cacheKey, items);
        }

        foreach (var item in items.Where(u => u.EntityNames != null && u.EntityNames.Contains(model.EntityValue.GetType())))
        {
            item.Execute(oldData, model);
        }
    }
}

public interface IDataExecuting
{
    List<Type> EntityNames { get; }

    void Execute(object oldData, DataFilterModel model);
}