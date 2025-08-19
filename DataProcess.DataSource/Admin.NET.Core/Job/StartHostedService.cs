// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 系统启动执行任务
/// </summary>
public class StartHostedService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();

            var originColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            // 枚举转字典
            var sysEnumService = serviceScope.ServiceProvider.GetRequiredService<SysEnumService>();
            await sysEnumService.EnumToDict();
            var message = $"【启动任务】系统枚举转换字典 {DateTime.Now}";
            Console.WriteLine(message);
            Log.Information(message);

            // 缓存租户列表
            await serviceScope.ServiceProvider.GetRequiredService<SysTenantService>().CacheTenant();
            message = $"【启动任务】缓存系统租户列表  {DateTime.Now}";
            Console.WriteLine(message);
            Log.Information(message);

            // 清理在线用户
            var db = serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>().CopyNew();
            await db.Deleteable<SysOnlineUser>().ExecuteCommandAsync(cancellationToken);
            message = $"【启动任务】清理系统在线用户 {DateTime.Now}";
            Console.WriteLine(message);
            Log.Information(message);

            Console.ForegroundColor = originColor;
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}