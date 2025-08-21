using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;
using DataProcess.DataSource.Application.Entity;

namespace DataProcess.DataSource.Application.Service.Plugin;

public class DataSourcePluginWatcherHostedService : BackgroundService
{
    private readonly ILogger<DataSourcePluginWatcherHostedService> _logger;
    private readonly PluginManager _pluginManager;
    private readonly ISqlSugarClient _db;
    private FileSystemWatcher? _watcher;
    private readonly string _pluginRoot;

    public DataSourcePluginWatcherHostedService(
        ILogger<DataSourcePluginWatcherHostedService> logger,
        PluginManager pluginManager,
        ISqlSugarClient db)
    {
        _logger = logger;
        _pluginManager = pluginManager;
        _db = db;
        _pluginRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "datasource");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_pluginRoot);

        // 启动时全量扫描
        await ScanAndRegisterAsync(stoppingToken);

        // 监听新增/删除
        _watcher = new FileSystemWatcher(_pluginRoot)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _watcher.Created += async (_, __) => await ScanAndRegisterAsync(stoppingToken);
        _watcher.Deleted += async (_, __) => await ScanAndRegisterAsync(stoppingToken);
    }

    private async Task ScanAndRegisterAsync(CancellationToken token)
    {
        try
        {
            if (token.IsCancellationRequested) return;

            var dirs = Directory.GetDirectories(_pluginRoot);
            foreach (var dir in dirs)
            {
                var pluginName = new DirectoryInfo(dir).Name;
                var info = await _pluginManager.GetPluginInfoAsync(pluginName);
                if (info == null) continue;

                var up = new DataSourceType
                {
                    Code = info.Code,
                    Name = info.Name,
                    Description = info.Description,
                    Version = info.Version,
                    AdapterClassName = info.AdapterClassName,
                    AssemblyName = pluginName,
                    ParamTemplate = info.ParamTemplate,
                    Icon = info.Icon,
                    IsBuiltIn = false,
                    Status = true,
                    UpdateTime = DateTime.Now
                };

                // 手工 Upsert（按 Code）
                var exist = await _db.Queryable<DataSourceType>().FirstAsync(x => x.Code == up.Code);
                if (exist == null)
                {
                    up.CreateTime = DateTime.Now;
                    await _db.Insertable(up).ExecuteCommandAsync();
                }
                else
                {
                    up.Id = exist.Id;
                    up.CreateTime = exist.CreateTime;
                    await _db.Updateable(up)
                        .IgnoreColumns(x => new { x.Id, x.CreateTime })
                        .ExecuteCommandAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描数据源插件失败");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
        return base.StopAsync(cancellationToken);
    }
}