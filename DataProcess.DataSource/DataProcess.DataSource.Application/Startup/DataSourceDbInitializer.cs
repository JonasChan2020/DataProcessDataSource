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
using System.Threading; // Ϊ WaitHandle ����

namespace DataProcess.DataSource.Application.Startup;

/// <summary>
/// ����Դģ�����ݿ��ʼ�����ӳ�ִ�У������ԣ�Ӧ�����������У�
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

        // �ȴ�Ӧ��������ȡ�����޸� CS1503��
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
                           ?? throw new InvalidOperationException("SqlSugar �� CurrentConnectionConfig Ϊ�գ���ȷ�� AddSqlSugar ��������ȷ���ء�");
                if (string.IsNullOrWhiteSpace(cfg.ConnectionString))
                    throw new InvalidOperationException("���ݿ������ַ���Ϊ�գ����������ļ��򻷾�������");

                db.DbMaintenance.CreateDatabase();
                try { db.Ado.Open(); } finally { db.Ado.Close(); }

                db.CodeFirst.InitTables(typeof(DataSourceType), typeof(DataSourceInstance));
                SeedRunner.Execute(db);

                _logger.LogInformation("[DataSource] ��ʼ����ɣ��� {Attempt}/{Max} �Σ���", attempt, maxRetry);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DataSource] �� {Attempt}/{Max} �γ�ʼ��ʧ�ܣ����� {Delay}ms �����ԡ�", attempt, maxRetry, delayMs);
                if (attempt == maxRetry)
                    _logger.LogError(ex, "[DataSource] �ﵽ������Դ�������������ʼ����");
                else
                {
                    try { await Task.Delay(delayMs, stoppingToken); } catch { /* ignore */ }
                }
            }
        }
    }
}