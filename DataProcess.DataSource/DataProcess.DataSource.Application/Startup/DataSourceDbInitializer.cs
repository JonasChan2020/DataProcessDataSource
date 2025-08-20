using Furion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using DataProcess.DataSource.Application.Entity;
using DbType = SqlSugar.DbType;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.SeedData;
using System.Threading; // 为 WaitHandle 引用

namespace DataProcess.DataSource.Application.Startup;

/// <summary>
/// 数据源模块数据库初始化（延迟执行，带重试，应用启动后运行）
/// </summary>
public class DataSourceDbInitializer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DataSourceDbInitializer> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    public DataSourceDbInitializer(
        IServiceProvider services,
        ILogger<DataSourceDbInitializer> logger,
        IHostApplicationLifetime appLifetime)
    {
        _services = services;
        _logger = logger;
        _appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        // 等待应用启动或被取消（修复 CS1503）
        await Task.Run(() =>
        {
            WaitHandle.WaitAny(new[]
            {
                _appLifetime.ApplicationStarted.WaitHandle,
                stoppingToken.WaitHandle
            });
        }, stoppingToken);

        const int maxRetry = 20;
        const int delayMs = 1500;

        for (var attempt = 1; attempt <= maxRetry && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

                var cfg = db.CurrentConnectionConfig
                           ?? throw new InvalidOperationException("SqlSugar 的 CurrentConnectionConfig 为空，请确认 AddSqlSugar 配置已正确加载。");
                if (string.IsNullOrWhiteSpace(cfg.ConnectionString))
                    throw new InvalidOperationException("数据库连接字符串为空，请检查配置文件或环境变量。");

                db.DbMaintenance.CreateDatabase();
                try { db.Ado.Open(); } finally { db.Ado.Close(); }

                db.CodeFirst.InitTables(typeof(DataSourceType), typeof(DataSourceInstance));
                SeedRunner.Execute(db);

                _logger.LogInformation("[DataSource] 初始化完成（第 {Attempt}/{Max} 次）。", attempt, maxRetry);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DataSource] 第 {Attempt}/{Max} 次初始化失败，将在 {Delay}ms 后重试。", attempt, maxRetry, delayMs);
                if (attempt == maxRetry)
                    _logger.LogError(ex, "[DataSource] 达到最大重试次数，已跳过初始化。");
                else
                {
                    try { await Task.Delay(delayMs, stoppingToken); } catch { /* ignore */ }
                }
            }
        }
    }
}