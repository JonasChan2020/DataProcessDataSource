using Furion;
using Furion.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcess.DataSource.Application.Startup;

[AppStartup(900)]
public class DataSourceAppStartup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 延迟初始化（应用启动完成后执行)
        services.AddHostedService<DataSourceDbInitializer>();

        // 插件管理与目录监听
        services.AddSingleton(ServiceDescriptor.Singleton(typeof(Service.Plugin.PluginManager), typeof(Service.Plugin.PluginManager)));
        services.AddHostedService<Service.Plugin.DataSourcePluginWatcherHostedService>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // no-op
    }
}