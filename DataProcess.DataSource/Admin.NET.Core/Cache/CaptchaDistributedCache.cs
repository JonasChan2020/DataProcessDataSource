// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Microsoft.Extensions.Caching.Distributed;

namespace Admin.NET.Core;

/// <summary>
/// 验证码分布式缓存（Redis模式）
/// </summary>
public class CaptchaDistributedCache : IDistributedCache
{
    private static ICacheProvider _cacheProvider;

    public CaptchaDistributedCache(ICacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }

    /// <summary>
    /// 验证码缓存前缀标识符
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private static string GetKey(string key)
    {
        return $"Captcha:{key}";
    }

    public byte[] Get(string key)
    {
        return _cacheProvider.Cache.Get<byte[]>(GetKey(key));
    }

    public Task<byte[]> GetAsync(string key, CancellationToken token = default)
    {
        return Task.FromResult(Get(key));
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpiration == null) return;
        TimeSpan timeDifference = options.AbsoluteExpiration.Value.Subtract(DateTimeOffset.Now);
        _cacheProvider.Cache.Set(GetKey(key), value, (int)timeDifference.TotalSeconds);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
        _cacheProvider.Cache.TryGetValue<byte[]>(key, out _);
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        Refresh(key);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _cacheProvider.Cache.Remove(GetKey(key));
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }
}