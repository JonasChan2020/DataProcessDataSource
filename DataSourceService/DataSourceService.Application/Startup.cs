using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Furion.DatabaseAccessor;

namespace DataSourceService.Application;

public class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSqlSugar();
    }

    public void Configure(IApplicationBuilder app, DataSourcePluginManager manager)
    {
        manager.LoadAll();
    }
}
