// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 清理日志作业任务（每天 00:00:00 执行）
/// </summary>
[JobDetail("job_log", Description = "清理操作日志", GroupName = "default", Concurrent = false)]
[Daily(TriggerId = "trigger_log", Description = "清理操作日志")]
public class LogJob(IServiceScopeFactory serviceScopeFactory) : IJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        using var serviceScope = _serviceScopeFactory.CreateScope();

        var db = serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>().CopyNew();
        var sysConfigService = serviceScope.ServiceProvider.GetRequiredService<SysConfigService>();

        // 日志保留天数
        var daysAgo = await sysConfigService.GetConfigValueByCode<int>(ConfigConst.SysLogRetentionDays);
        // 删除访问日志
        await db.Deleteable<SysLogVis>().Where(u => u.CreateTime < DateTime.Now.AddDays(-daysAgo)).ExecuteCommandAsync(stoppingToken);
        // 删除操作日志
        await db.Deleteable<SysLogOp>().Where(u => u.CreateTime < DateTime.Now.AddDays(-daysAgo)).ExecuteCommandAsync(stoppingToken);
        // 删除差异日志
        await db.Deleteable<SysLogDiff>().Where(u => u.CreateTime < DateTime.Now.AddDays(-daysAgo)).ExecuteCommandAsync(stoppingToken);
        // 删除作业触发器运行记录
        await db.Deleteable<SysJobTriggerRecord>().Where(u => u.CreatedTime < DateTime.Now.AddDays(-daysAgo)).ExecuteCommandAsync(stoppingToken);

        var originColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        var message = $"【定时任务】清理系统日志成功，清理 {daysAgo} 天前的日志数据 {DateTime.Now}";
        Console.WriteLine(message);
        Log.Information(message);
        Console.ForegroundColor = originColor;
    }
}