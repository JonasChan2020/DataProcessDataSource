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

        // �ȴ�Ӧ��������ȡ������������ʼ����
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

                // �ؼ��޸��㣺ͨ�����û�ȡһ����Ч�� Provider����Ҫ���� ITenant.ConnectionConfigs��
                var provider = GetProviderOrThrow(db);

                // ��¼��ǰ������Ϣ��������
                var cfg = provider.CurrentConnectionConfig;
                _logger.LogInformation("[DataSource] ʹ������ ConfigId={ConfigId}, DbType={DbType}, Conn={Conn}",
                    cfg.ConfigId, cfg.DbType, MaskConn(cfg.ConnectionString));

                // ���⣨�������ݿⲻ֧���򲶻�澯��
                try
                {
                    provider.DbMaintenance.CreateDatabase();
                }
                catch (Exception ce)
                {
                    _logger.LogWarning(ce, "[DataSource] CreateDatabase ���棨�����Ѵ��ڻ��ṩ�߲�֧���Զ����⣩��");
                }

                // ��������
                try { provider.Ado.Open(); }
                catch (Exception openEx)
                {
                    throw new InvalidOperationException("�޷������ݿ����ӣ����� AddSqlSugar �����������ַ����Ƿ���ȷ��", openEx);
                }
                finally { try { provider.Ado.Close(); } catch { /* ignore */ } }

                // ����
                provider.CodeFirst.InitTables(typeof(DataSourceType), typeof(DataSourceInstance));

                // ִ�����ӣ���ͬһ Provider �ϣ�
                SeedRunner.Execute(provider);

                _logger.LogInformation("[DataSource] ��ʼ����ɣ��� {Attempt}/{Max} �Σ���", attempt, maxRetry);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DataSource] �� {Attempt}/{Max} �γ�ʼ��ʧ�ܣ����� {Delay}ms �����ԡ�", attempt, maxRetry, delayMs);
                if (attempt == maxRetry)
                {
                    _logger.LogError(ex, "[DataSource] �ﵽ������Դ�������������ʼ����");
                }
                else
                {
                    try { await Task.Delay(delayMs, stoppingToken); } catch { /* ignore */ }
                }
            }
        }
    }

    // ͨ�����û�ȡһ����Ч Provider��Ĭ��ȡ��һ������
    private static SqlSugarScopeProvider GetProviderOrThrow(ISqlSugarClient db)
    {
        var tenant = db.AsTenant() ?? throw new InvalidOperationException("SqlSugar δ���ö�����/���⻧��AsTenant Ϊ null����");

        // �����ö�ȡ���Ӽ��ϣ��� Web.Core Startup ��ע����Դһ�£�
        var cfgs = Furion.App.GetConfig<List<ConnectionConfig>>("ConnectionConfigs")
                   ?? throw new InvalidOperationException("δ��ȡ�� ConnectionConfigs ���ã����������ļ�����ӡ�");

        if (cfgs.Count == 0)
            throw new InvalidOperationException("ConnectionConfigs ����Ϊ�գ�������Ҫһ�����ӡ�");

        // ѡȡ��һ����Ч����
        var first = cfgs.First();
        var configId = first.ConfigId?.ToString();
        if (string.IsNullOrWhiteSpace(configId))
            configId = "main"; // �� Web.Core �еĶ��� ConfigId һ��

        var provider = tenant.GetConnectionScope(configId)
                      ?? throw new InvalidOperationException($"�޷���ȡ����������ConfigId={configId}");
        return provider;
    }

    private static string MaskConn(string? conn)
    {
        if (string.IsNullOrWhiteSpace(conn)) return "<empty>";
        var s = conn.Replace("\r", "").Replace("\n", "");
        return s.Length <= 12 ? "****" : $"{s[..4]}****{s[^4..]}";
    }
}