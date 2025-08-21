using Furion;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcess.DataSource.Application.Startup;

[AppStartup(900)]
public class DataSourceAppStartup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<DataSourceDbInitializer>();
        services.AddSingleton<Service.Plugin.PluginManager>();
        services.AddHostedService<Service.Plugin.DataSourcePluginWatcherHostedService>();
    }

    public void Configure(IApplicationBuilder app) { }
}