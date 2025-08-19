// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统缓存服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 400, Description = "系统缓存")]
public class SysCacheService : IDynamicApiController, ISingleton
{
    private static ICacheProvider _cacheProvider;
    private readonly CacheOptions _cacheOptions;

    public SysCacheService(ICacheProvider cacheProvider, IOptions<CacheOptions> cacheOptions)
    {
        _cacheProvider = cacheProvider;
        _cacheOptions = cacheOptions.Value;
    }

    /// <summary>
    /// 申请分布式锁 🔖
    /// </summary>
    /// <param name="key">要锁定的key</param>
    /// <param name="msTimeout">申请锁等待的时间，单位毫秒</param>
    /// <param name="msExpire">锁过期时间，超过该时间没有主动是放则自动是放，必须整数秒，单位毫秒</param>
    /// <param name="throwOnFailure">失败时是否抛出异常,如不抛出异常，可通过判断返回null得知申请锁失败</param>
    /// <returns></returns>
    [DisplayName("申请分布式锁")]
    public IDisposable? BeginCacheLock(string key, int msTimeout = 500, int msExpire = 10000, bool throwOnFailure = true)
    {
        try
        {
            return _cacheProvider.Cache.AcquireLock(key, msTimeout, msExpire, throwOnFailure);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取缓存键名集合 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取缓存键名集合")]
    public List<string> GetKeyList()
    {
        return _cacheProvider.Cache == Cache.Default
            ? [.. _cacheProvider.Cache.Keys.Where(u => u.StartsWith(_cacheOptions.Prefix)).Select(u => u[_cacheOptions.Prefix.Length..]).OrderBy(u => u)]
            : [.. ((FullRedis)_cacheProvider.Cache).Search($"{_cacheOptions.Prefix}*", int.MaxValue).Select(u => u[_cacheOptions.Prefix.Length..]).OrderBy(u => u)];
    }

    /// <summary>
    /// 增加缓存
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [NonAction]
    public bool Set(string key, object value)
    {
        return !string.IsNullOrWhiteSpace(key) && _cacheProvider.Cache.Set($"{_cacheOptions.Prefix}{key}", value);
    }

    /// <summary>
    /// 增加缓存并设置过期时间
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    [NonAction]
    public bool Set(string key, object value, TimeSpan expire)
    {
        return !string.IsNullOrWhiteSpace(key) && _cacheProvider.Cache.Set($"{_cacheOptions.Prefix}{key}", value, expire);
    }

    public async Task<TR> AdGetAsync<TR>(String cacheName, Func<Task<TR>> del, TimeSpan? expiry = default) where TR : class
    {
        return await AdGetAsync<TR>(cacheName, del, [], expiry);
    }

    public async Task<TR> AdGetAsync<TR, T1>(String cacheName, Func<T1, Task<TR>> del, T1 t1, TimeSpan? expiry = default) where TR : class
    {
        return await AdGetAsync<TR>(cacheName, del, [t1], expiry);
    }

    public async Task<TR> AdGetAsync<TR, T1, T2>(String cacheName, Func<T1, T2, Task<TR>> del, T1 t1, T2 t2, TimeSpan? expiry = default) where TR : class
    {
        return await AdGetAsync<TR>(cacheName, del, [t1, t2], expiry);
    }

    public async Task<TR> AdGetAsync<TR, T1, T2, T3>(String cacheName, Func<T1, T2, T3, Task<TR>> del, T1 t1, T2 t2, T3 t3, TimeSpan? expiry = default) where TR : class
    {
        return await AdGetAsync<TR>(cacheName, del, [t1, t2, t3], expiry);
    }

    private async Task<T> AdGetAsync<T>(string cacheName, Delegate del, Object[] obs, TimeSpan? expiry) where T : class
    {
        var key = Key(cacheName, obs);
        // 使用分布式锁
        using (_cacheProvider.Cache.AcquireLock($@"lock:AdGetAsync:{cacheName}", 1000))
        {
            var value = Get<T>(key);
            value ??= await ((dynamic)del).DynamicInvokeAsync(obs);
            Set(key, value);
            return value;
        }
    }

    public T Get<T>(String cacheName, object t1)
    {
        return Get<T>(cacheName, [t1]);
    }

    public T Get<T>(String cacheName, object t1, object t2)
    {
        return Get<T>(cacheName, [t1, t2]);
    }

    public T Get<T>(String cacheName, object t1, object t2, object t3)
    {
        return Get<T>(cacheName, [t1, t2, t3]);
    }

    private T Get<T>(String cacheName, Object[] obs)
    {
        var key = cacheName + ":" + obs.Aggregate(string.Empty, (current, o) => current + $"<{o}>");
        return Get<T>(key);
    }

    private static string Key(string cacheName, object[] obs)
    {
        if (obs.OfType<TimeSpan>().Any()) throw new Exception("缓存参数类型不能能是:TimeSpan类型");
        StringBuilder sb = new(cacheName + ":");
        foreach (var a in obs) sb.Append($"<{KeySingle(a)}>");
        return sb.ToString();
    }

    private static string KeySingle(object t)
    {
        return t.GetType().IsClass && !t.GetType().IsPrimitive ? JSON.Serialize(t) : t.ToString();
    }

    /// <summary>
    /// 获取缓存的剩余生存时间
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [NonAction]
    public static TimeSpan GetExpire(string key)
    {
        return _cacheProvider.Cache.GetExpire(key);
    }

    /// <summary>
    /// 获取缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    [NonAction]
    public T Get<T>(string key)
    {
        return _cacheProvider.Cache.Get<T>($"{_cacheOptions.Prefix}{key}");
    }

    /// <summary>
    /// 批量获取缓存值（普通键值结构）🔖
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="keys">缓存键集合</param>
    /// <returns>与键顺序对应的值列表</returns>
    [NonAction]
    public List<T> GetBatch<T>(IEnumerable<string> keys)
    {
        var prefixedKeys = keys.Select(k => $"{_cacheOptions.Prefix}{k}");
        return prefixedKeys.Select(k => _cacheProvider.Cache.Get<T>(k)).ToList();
    }

    /// <summary>
    /// 批量设置缓存项（兼容现有键规则）❄️，方法只对雪花Id有效
    /// </summary>
    /// <typeparam name="T">实体类型（需包含Id属性）</typeparam>
    /// <param name="items">待缓存数据集合</param>
    /// <param name="expire">统一过期时间</param>
    /// <param name="batchSize">批次大小（默认500）</param>
    [NonAction]
    public void SetList<T>(IEnumerable<T> items, TimeSpan? expire = null, int batchSize = 500) where T : class
    {
        if (items == null) return;

        var itemList = items.ToList();
        if (itemList.Count == 0) return;

        // 获取雪花ID属性
        var idProperty = typeof(T).GetProperty("Id")
            ?? throw new ArgumentException("实体必须包含Id属性");

        // 分批次处理
        foreach (var batch in itemList.Batch(batchSize))
        {
            var dic = batch.ToDictionary(
                item => $"{_cacheOptions.Prefix}{idProperty.GetValue(item)}",
                item => item
            );

            if (_cacheProvider.Cache is Redis redis)
            {
                // Redis管道批量设置
                redis.StartPipeline();
                try
                {
                    foreach (var kv in dic)
                    {
                        redis.Set(kv.Key, kv.Value, expire ?? TimeSpan.Zero);
                    }
                }
                finally
                {
                    redis.StopPipeline(true);
                }
            }
            else
            {
                // 通用缓存实现
                foreach (var kv in dic)
                {
                    _cacheProvider.Cache.Set(kv.Key, kv.Value, expire ?? TimeSpan.Zero);
                }
            }
        }
    }

    /// <summary>
    /// 异步批量获取（当前为同步实现，未来可升级）
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="ids">雪花ID集合</param>
    /// <param name="loadFromDb">数据加载方法</param>
    /// <param name="cacheNull">是否缓存空值（防穿透）</param>
    /// <param name="nullExpire">空值缓存时间（默认永久）</param>
    /// </summary>
    [NonAction]
    public async Task<List<T>> GetListAsync<T>(IEnumerable<long> ids, Func<List<long>, Task<List<T>>> loadFromDb, bool cacheNull = true, TimeSpan? nullExpire = null) where T : class
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];

        // 1. 批量获取缓存（保持同步，假设缓存操作快速）
        var cachedItems = GetFromCache<T>(idList);

        // 2. 识别未命中ID
        var missedIds = new List<long>();
        var resultDict = new Dictionary<long, T>();

        for (int i = 0; i < idList.Count; i++)
        {
            if (cachedItems[i] != null)
            {
                resultDict[idList[i]] = cachedItems[i];
            }
            else
            {
                missedIds.Add(idList[i]);
            }
        }

        // 3. 异步加载缺失数据
        if (missedIds.Count > 0)
        {
            var dbItems = await loadFromDb(missedIds).ConfigureAwait(false);  // 异步等待
            var dbDict = dbItems.ToDictionary(GetId);

            // 4. 缓存回填
            var toCache = new List<T>();
            foreach (var id in missedIds)
            {
                if (dbDict.TryGetValue(id, out var item))
                {
                    resultDict[id] = item;
                    toCache.Add(item);
                }
                //else if (cacheNull)
                //{
                //    // 使用 default(T) 作为空值标记
                //    toCache.Add(default(T));
                //}
            }

            if (toCache.Count > 0) SetList(toCache, cacheNull ? nullExpire : null);  // 保持同步缓存写入
        }

        // 5. 按原始顺序返回
        return idList.Select(id => resultDict.TryGetValue(id, out var item)
            ? (item ?? null)
            : null).ToList();
    }

    /// <summary>
    /// 批量获取（自动加载缺失数据+缓存回填）🔁
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="ids">雪花ID集合</param>
    /// <param name="loadFromDb">数据加载方法</param>
    /// <param name="cacheNull">是否缓存空值（防穿透）</param>
    /// <param name="nullExpire">空值缓存时间（默认永久）</param>
    [NonAction]
    public List<T> GetList<T>(IEnumerable<long> ids, Func<List<long>, List<T>> loadFromDb, bool cacheNull = true, TimeSpan? nullExpire = null) where T : class
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];

        // 1. 批量获取缓存
        var cachedItems = GetFromCache<T>(idList);

        // 2. 识别未命中ID
        var missedIds = new List<long>();
        var resultDict = new Dictionary<long, T>();

        for (int i = 0; i < idList.Count; i++)
        {
            if (cachedItems[i] != null)
            {
                resultDict[idList[i]] = cachedItems[i];
            }
            else
            {
                missedIds.Add(idList[i]);
            }
        }

        // 3. 加载缺失数据
        if (missedIds.Count > 0)
        {
            var dbItems = loadFromDb(missedIds);
            var dbDict = dbItems.ToDictionary(GetId);

            // 4. 缓存回填
            var toCache = new List<T>();
            foreach (var id in missedIds)
            {
                if (dbDict.TryGetValue(id, out var item))
                {
                    resultDict[id] = item;
                    toCache.Add(item);
                }
                //else if (cacheNull)
                //{
                //    // 缓存空值标记
                //    toCache.Add(default(T));
                //}
            }

            //SetList(toCache, cacheNull ? (nullExpire ?? TimeSpan.FromMinutes(5)) : null);
            // 将默认过期时间改为null（一直存储）

            if (toCache.Count > 0) SetList(toCache, cacheNull ? nullExpire : null);
        }

        // 5. 按原始顺序返回
        return idList.Select(id => resultDict.TryGetValue(id, out var item)
            ? (item ?? null)
            : null).ToList();
    }

    private long GetId<T>(T item)
    {
        var prop = typeof(T).GetProperty("Id");
        return (long)prop.GetValue(item);
    }

    /// <summary>
    /// 基础方法：仅从缓存获取数据
    /// </summary>
    [NonAction]
    public List<T> GetFromCache<T>(List<long> ids) where T : class
    {
        if (ids == null || ids.Count == 0) return [];

        var keys = ids.Select(id => $"{_cacheOptions.Prefix}{id}").ToList();
        if (_cacheProvider.Cache is FullRedis redis)
        {
            var result = redis.GetAll<T>(keys);
            return keys.Select(k => result.TryGetValue(k, out var val) ? val : null).ToList();
        }

        return keys.Select(k => _cacheProvider.Cache.Get<T>(k)).ToList();
    }

    /// <summary>
    /// 批量获取哈希缓存字段值（哈希结构）🔖
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">哈希键名</param>
    /// <param name="fields">要获取的字段集合</param>
    /// <returns>与字段顺序对应的值列表</returns>
    [NonAction]
    public List<T> HashGetBatch<T>(string key, IEnumerable<string> fields)
    {
        var hash = GetHashMap<T>($"{_cacheOptions.Prefix}{key}");
        return fields.Select(f => hash.TryGetValue(f, out T val) ? val : default).ToList();
    }

    /// <summary>
    /// 删除缓存 🔖
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除缓存")]
    public int Remove(string key)
    {
        return _cacheProvider.Cache.Remove($"{_cacheOptions.Prefix}{key}");
    }

    /// <summary>
    /// 清空所有缓存 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("清空所有缓存")]
    [ApiDescriptionSettings(Name = "Clear"), HttpPost]
    public void Clear()
    {
        _cacheProvider.Cache.Clear();

        Cache.Default.Clear();
    }

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    /// <param name="key">键</param>
    /// <returns></returns>
    [NonAction]
    public bool ExistKey(string key)
    {
        return _cacheProvider.Cache.ContainsKey($"{_cacheOptions.Prefix}{key}");
    }

    /// <summary>
    /// 根据键名前缀删除缓存 🔖
    /// </summary>
    /// <param name="prefixKey">键名前缀</param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "DeleteByPreKey"), HttpPost]
    [DisplayName("根据键名前缀删除缓存")]
    public int RemoveByPrefixKey(string prefixKey)
    {
        var delKeys = _cacheProvider.Cache == Cache.Default
            ? _cacheProvider.Cache.Keys.Where(u => u.StartsWith($"{_cacheOptions.Prefix}{prefixKey}")).ToArray()
            : ((FullRedis)_cacheProvider.Cache).Search($"{_cacheOptions.Prefix}{prefixKey}*", int.MaxValue).ToArray();
        return _cacheProvider.Cache.Remove(delKeys);
    }

    /// <summary>
    /// 根据键名前缀获取键名集合 🔖
    /// </summary>
    /// <param name="prefixKey">键名前缀</param>
    /// <returns></returns>
    [DisplayName("根据键名前缀获取键名集合")]
    public List<string> GetKeysByPrefixKey(string prefixKey)
    {
        return _cacheProvider.Cache == Cache.Default
            ? _cacheProvider.Cache.Keys.Where(u => u.StartsWith($"{_cacheOptions.Prefix}{prefixKey}")).Select(u => u[_cacheOptions.Prefix.Length..]).ToList()
            : ((FullRedis)_cacheProvider.Cache).Search($"{_cacheOptions.Prefix}{prefixKey}*", int.MaxValue).Select(u => u[_cacheOptions.Prefix.Length..]).ToList();
    }

    /// <summary>
    /// 获取缓存值 🔖
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [DisplayName("获取缓存值")]
    public object GetValue(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        // 若Key经过URL编码则进行解码
        if (Regex.IsMatch(key, @"%[0-9a-fA-F]{2}"))
            key = HttpUtility.UrlDecode(key);

        var fullKey = $"{_cacheOptions.Prefix}{key}";

        if (_cacheProvider.Cache == Cache.Default)
            return _cacheProvider.Cache.Get<object>(fullKey);

        if (_cacheProvider.Cache is FullRedis redisCache)
        {
            if (!redisCache.ContainsKey(fullKey))
                return null;
            try
            {
                var keyType = redisCache.TYPE(fullKey)?.ToLower();
                switch (keyType)
                {
                    case "string":
                        return redisCache.Get<string>(fullKey);

                    case "list":
                        var list = redisCache.GetList<string>(fullKey);
                        return list?.ToList();

                    case "hash":
                        var hash = redisCache.GetDictionary<string>(fullKey);
                        return hash?.ToDictionary(k => k.Key, v => v.Value);

                    case "set":
                        var set = redisCache.GetSet<string>(fullKey);
                        return set?.ToArray();

                    case "zset":
                        var sortedSet = redisCache.GetSortedSet<string>(fullKey);
                        return sortedSet?.Range(0, -1)?.ToList();

                    case "none":
                        return null;

                    default:
                        // 未知类型或特殊类型
                        return new Dictionary<string, object>
                        {
                            { "key", key },
                            { "type", keyType ?? "unknown" },
                            { "message", "无法使用标准方式获取此类型数据" }
                        };
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    { "key", key },
                    { "error", ex.Message },
                    { "type", "exception" }
                };
            }
        }

        return _cacheProvider.Cache.Get<object>(fullKey);
    }

    /// <summary>
    /// 获取或添加缓存（在数据不存在时执行委托请求数据）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="callback"></param>
    /// <param name="expire">过期时间，单位秒</param>
    /// <returns></returns>
    [NonAction]
    public T GetOrAdd<T>(string key, Func<string, T> callback, int expire = -1)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;
        return _cacheProvider.Cache.GetOrAdd($"{_cacheOptions.Prefix}{key}", callback, expire);
    }

    /// <summary>
    /// Hash匹配
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    [NonAction]
    public static IDictionary<String, T> GetHashMap<T>(string key)
    {
        return _cacheProvider.Cache.GetDictionary<T>(key);
    }

    /// <summary>
    /// 批量添加HASH
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="dic"></param>
    /// <returns></returns>
    [NonAction]
    public static bool HashSet<T>(string key, Dictionary<string, T> dic)
    {
        var hash = GetHashMap<T>(key);
        foreach (var v in dic)
        {
            hash.Add(v);
        }
        return true;
    }

    /// <summary>
    /// 添加一条HASH
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="hashKey"></param>
    /// <param name="value"></param>
    [NonAction]
    public static void HashAdd<T>(string key, string hashKey, T value)
    {
        var hash = GetHashMap<T>(key);
        hash.Add(hashKey, value);
    }

    /// <summary>
    /// 添加或更新一条HASH
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="hashKey"></param>
    /// <param name="value"></param>
    [NonAction]
    public static void HashAddOrUpdate<T>(string key, string hashKey, T value)
    {
        var hash = GetHashMap<T>(key);
        if (hash.ContainsKey(hashKey))
            hash[hashKey] = value;
        else
            hash.Add(hashKey, value);
    }

    /// <summary>
    /// 获取多条HASH
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="fields"></param>
    /// <returns></returns>
    [NonAction]
    public static List<T> HashGet<T>(string key, params string[] fields)
    {
        var hash = GetHashMap<T>(key);
        return hash.Where(t => fields.Any(c => t.Key == c)).Select(t => t.Value).ToList();
    }

    /// <summary>
    /// 获取一条HASH
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    [NonAction]
    public static T HashGetOne<T>(string key, string field)
    {
        var hash = GetHashMap<T>(key);
        return hash.TryGetValue(field, out T value) ? value : default;
    }

    // 新增方法：获取哈希表所有键
    public static List<string> HashGetAllKeys(string key)
    {
        var hash = GetHashMap<string>(key); // 假设值为任意类型
        return hash.Keys.ToList();
    }

    // 增强的哈希设置方法（带过期时间）
    public static bool HashSet<T>(string key, Dictionary<string, T> items, TimeSpan? expiry = null)
    {
        var hash = GetHashMap<T>(key);
        foreach (var item in items)
        {
            hash[item.Key] = item.Value;
        }

        if (expiry.HasValue)
        {
            _cacheProvider.Cache.SetExpire(key, expiry.Value);
        }
        return true;
    }

    // 异步批量设置哈希，目前没有，先保留扩展
    public static async Task<bool> HashSetAsync<T>(string key, Dictionary<string, T> items, TimeSpan? expiry = null)
    {
        var hash = GetHashMap<T>(key);
        foreach (var item in items)
        {
            hash[item.Key] = item.Value;
        }
        if (expiry.HasValue)
        {
            _cacheProvider.Cache.SetExpire(key, expiry.Value);
        }
        await Task.CompletedTask;
        return true;
    }

    // 异步设置过期时间

    /// <summary>
    /// 根据KEY获取所有HASH
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    [NonAction]
    public static IDictionary<string, T> HashGetAll<T>(string key)
    {
        var hash = GetHashMap<T>(key);
        return hash;
    }

    /// <summary>
    /// 删除HASH
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="fields"></param>
    /// <returns></returns>
    [NonAction]
    public static int HashDel<T>(string key, params string[] fields)
    {
        var hash = GetHashMap<T>(key);
        fields.ToList().ForEach(t => hash.Remove(t));
        return fields.Length;
    }

    ///// <summary>
    ///// 搜索HASH
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    ///// <param name="key"></param>
    ///// <param name="searchModel"></param>
    ///// <returns></returns>
    //[NonAction]
    //public List<KeyValuePair<string, T>> HashSearch<T>(string key, SearchModel searchModel)
    //{
    //    var hash = GetHashMap<T>(key);
    //    return hash.Search(searchModel).ToList();
    //}

    ///// <summary>
    ///// 搜索HASH
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    ///// <param name="key"></param>
    ///// <param name="pattern"></param>
    ///// <param name="count"></param>
    ///// <returns></returns>
    //[NonAction]
    //public List<KeyValuePair<string, T>> HashSearch<T>(string key, string pattern, int count)
    //{
    //    var hash = GetHashMap<T>(key);
    //    return hash.Search(pattern, count).ToList();
    //}
}

public class CacheItem<T>
{
    public T Value { get; set; }
    public bool IsNull { get; set; }
}