// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// YitIdHelper 自动获取WorkId拓展（支持分布式部署）
/// </summary>
public static class YitIdHelperExtension
{
    private const string MainLockName = "sys_idGen:workerId:lock";
    private const string MainValueKey = "sys_idGen:workerId:value";

    private static readonly List<string> _workIds = [];
    private static SnowIdOptions _options;

    public static void AddYitIdHelper(this IServiceCollection services, SnowIdOptions options)
    {
        if (App.GetConfig<CacheOptions>("Cache", true).CacheType == CacheTypeEnum.Memory.ToString())
        {
            YitIdHelper.SetIdGenerator(options);
            SnowFlakeSingle.WorkId = options.WorkerId;
            Console.WriteLine($"############ 当前应用雪花WorkId:【{options.WorkerId}】############");
            return;
        }

        _options = options;

        // 排除开发环境和Windows服务器
        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || App.WebHostEnvironment.IsDevelopment())
        //{
        //    YitIdHelper.SetIdGenerator(_options);
        //    return;
        //}

        var maxLength = Math.Pow(2, _options.WorkerIdBitLength.ToLong());
        for (int i = 0; i < maxLength; i++)
        {
            _workIds.Add(i.ToString());
        }

        Random ran = new();
        Thread.Sleep(ran.Next(10, 1000));

        SetWorkId();
    }

    private static void SetWorkId()
    {
        var lockName = $"{_options.WorkerPrefix}{MainLockName}";
        var valueKey = $"{_options.WorkerPrefix}{MainValueKey}";

        var minWorkId = 0;
        var maxWorkId = Math.Pow(2, _options.WorkerIdBitLength.ToLong());

        var cache = App.GetRequiredService<ICacheProvider>().Cache;
        var redisLock = cache.AcquireLock(lockName, 10000, 15000, true);
        var keys = cache == Cache.Default
            ? cache.Keys.Where(u => u.StartsWith($"{_options.WorkerPrefix}{valueKey}:*"))
            : ((FullRedis)cache).Search($"{_options.WorkerPrefix}{valueKey}:*", int.MaxValue);

        var tempWorkIds = _workIds;
        foreach (var key in keys)
        {
            var tempWorkId = key[key.LastIndexOf(':')..];
            tempWorkIds.Remove(tempWorkId);
        }

        try
        {
            string workIdKey = "";
            foreach (var item in tempWorkIds)
            {
                var workIdStr = item;
                workIdKey = $"{valueKey}:{workIdStr}";
                var exist = cache.Get<bool>(workIdKey);
                if (exist)
                {
                    workIdKey = "";
                    continue;
                }

                Console.WriteLine($"############ 当前应用雪花WorkId:【{workIdStr}】############");

                long workId = workIdStr.ToLong();
                if (workId < minWorkId || workId > maxWorkId)
                    continue;

                // 设置雪花Id算法机器码
                YitIdHelper.SetIdGenerator(new IdGeneratorOptions
                {
                    WorkerId = (ushort)workId,
                    WorkerIdBitLength = _options.WorkerIdBitLength,
                    SeqBitLength = _options.SeqBitLength
                });

                cache.Set(workIdKey, true, TimeSpan.FromSeconds(15));
                break;
            }

            if (string.IsNullOrWhiteSpace(workIdKey)) throw Oops.Oh("未设置有效的机器码，启动失败");

            // 开一个任务设置当前workId过期时间
            Task.Run(() =>
            {
                while (true)
                {
                    cache.SetExpire(workIdKey, TimeSpan.FromSeconds(15));
                    // Task.Delay(5000);
                    Thread.Sleep(10000);
                }
            });
        }
        catch (Exception ex)
        {
            throw Oops.Oh($"{ex.Message};{ex.StackTrace};{ex.StackTrace}");
        }
        finally
        {
            redisLock?.Dispose();
        }
    }
}