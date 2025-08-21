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

    public DataSourceDbInitializer(IServiceProvider services, ILogger<DataSourceDbInitializer> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        const int maxRetry = 20;
        const int delayMs = 1500;

        for (var attempt = 1; attempt <= maxRetry && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetService<ISqlSugarClient>() ?? throw new InvalidOperationException("ISqlSugarClient δ����");

                // ����
                db.CodeFirst.InitTables<DataSourceType, DataSourceInstance>();

                // ȫ���������� Upsert��ö�� SqlSugar.DbType���������ⲿ���ã�
                var adapterClass = "DataProcess.DataSource.Application.Service.Adapter.SqlSugarDataSourceAdapter";
                var asmName = typeof(DataSourceDbInitializer).Assembly.GetName().Name ?? "DataProcess.DataSource.Application";

                var all = Enum.GetValues(typeof(DbType)).Cast<DbType>().ToList();
                var list = new List<DataSourceType>();
                var order = 1;
                foreach (var v in all)
                {
                    var code = v.ToString();
                    list.Add(new DataSourceType
                    {
                        Code = code,
                        Name = code,
                        Description = $"���� {code} ����Դ",
                        Version = "1.0",
                        AdapterClassName = adapterClass,
                        AssemblyName = asmName,
                        ParamTemplate = JSON.Serialize(new { ConnectionString = "", DbType = code }),
                        Icon = "",
                        IsBuiltIn = true,
                        OrderNo = order++,
                        Status = true,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    });
                }

                // �� Code ΪΨһ�������� Upsert
                var storage = db.Storageable(list).WhereColumns(x => x.Code).ToStorage();
                var ins = storage.AsInsertable.ExecuteCommand();
                var upd = storage.AsUpdateable.IgnoreColumns(x => new { x.Id, x.CreateTime }).ExecuteCommand();

                _logger.LogInformation("[DataSource] ����Դ����������ɣ��� {Total}������ {Ins}������ {Upd}", list.Count, ins, upd);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DataSource] ��ʼ��ʧ�ܣ��� {Attempt}/{Max} �Σ���{Delay}ms �����ԡ�", attempt, maxRetry, delayMs);
                if (attempt == maxRetry) _logger.LogError(ex, "[DataSource] �ﵽ������Դ�������������ʼ����");
                else try { await Task.Delay(delayMs, stoppingToken); } catch { }
            }
        }
    }
}