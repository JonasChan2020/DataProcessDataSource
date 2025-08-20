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
using System.Threading;
using System.Linq; // First/Select
using System.Collections.Generic; // List

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

        // 等待应用启动或被取消（避免过早初始化）
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

                // 关键修复点：通过配置获取一个有效的 Provider（不要访问 ITenant.ConnectionConfigs）
                var provider = GetProviderOrThrow(db);

                // 记录当前连接信息（脱敏）
                var cfg = provider.CurrentConnectionConfig;
                _logger.LogInformation("[DataSource] 使用连接 ConfigId={ConfigId}, DbType={DbType}, Conn={Conn}",
                    cfg.ConfigId, cfg.DbType, MaskConn(cfg.ConnectionString));

                // 建库（部分数据库不支持则捕获告警）
                try
                {
                    provider.DbMaintenance.CreateDatabase();
                }
                catch (Exception ce)
                {
                    _logger.LogWarning(ce, "[DataSource] CreateDatabase 警告（可能已存在或提供者不支持自动建库）。");
                }

                // 测试连接
                try { provider.Ado.Open(); }
                catch (Exception openEx)
                {
                    throw new InvalidOperationException("无法打开数据库连接，请检查 AddSqlSugar 配置与连接字符串是否正确。", openEx);
                }
                finally { try { provider.Ado.Close(); } catch { /* ignore */ } }

                // 建表
                provider.CodeFirst.InitTables(typeof(DataSourceType), typeof(DataSourceInstance));

                // 执行种子（在同一 Provider 上）
                SeedRunner.Execute(provider);

                _logger.LogInformation("[DataSource] 初始化完成（第 {Attempt}/{Max} 次）。", attempt, maxRetry);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DataSource] 第 {Attempt}/{Max} 次初始化失败，将在 {Delay}ms 后重试。", attempt, maxRetry, delayMs);
                if (attempt == maxRetry)
                {
                    _logger.LogError(ex, "[DataSource] 达到最大重试次数，已跳过初始化。");
                }
                else
                {
                    try { await Task.Delay(delayMs, stoppingToken); } catch { /* ignore */ }
                }
            }
        }
    }

    // 通过配置获取一个有效 Provider；默认取第一个连接
    private static SqlSugarScopeProvider GetProviderOrThrow(ISqlSugarClient db)
    {
        var tenant = db.AsTenant() ?? throw new InvalidOperationException("SqlSugar 未启用多连接/多租户（AsTenant 为 null）。");

        // 从配置读取连接集合（与 Web.Core Startup 的注册来源一致）
        var cfgs = Furion.App.GetConfig<List<ConnectionConfig>>("ConnectionConfigs")
                   ?? throw new InvalidOperationException("未读取到 ConnectionConfigs 配置，请在配置文件中添加。");

        if (cfgs.Count == 0)
            throw new InvalidOperationException("ConnectionConfigs 配置为空，至少需要一个连接。");

        // 选取第一个有效配置
        var first = cfgs.First();
        var configId = first.ConfigId?.ToString();
        if (string.IsNullOrWhiteSpace(configId))
            configId = "main"; // 与 Web.Core 中的兜底 ConfigId 一致

        var provider = tenant.GetConnectionScope(configId)
                      ?? throw new InvalidOperationException($"无法获取连接作用域：ConfigId={configId}");
        return provider;
    }

    private static string MaskConn(string? conn)
    {
        if (string.IsNullOrWhiteSpace(conn)) return "<empty>";
        var s = conn.Replace("\r", "").Replace("\n", "");
        return s.Length <= 12 ? "****" : $"{s[..4]}****{s[^4..]}";
    }
}