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
        // �ӳٳ�ʼ����Ӧ��������ɺ�ִ��)
        services.AddHostedService<DataSourceDbInitializer>();

        // ���������Ŀ¼����
        services.AddSingleton(ServiceDescriptor.Singleton(typeof(Service.Plugin.PluginManager), typeof(Service.Plugin.PluginManager)));
        services.AddHostedService<Service.Plugin.DataSourcePluginWatcherHostedService>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // no-op
    }
}