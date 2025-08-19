// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Furion;
using Microsoft.AspNetCore.Http.Features;
using System.Reflection;

Serve.Run(RunOptions.Default.AddWebComponent<WebComponent>().UseComponent<ApplicationComponent>());

public class WebComponent : IWebComponent
{
    public void Load(WebApplicationBuilder builder, ComponentContext componentContext)
    {
        // 设置日志过滤
        builder.Logging.AddFilter((provider, category, logLevel) =>
        {
            return !new[] { "Microsoft.Hosting", "Microsoft.AspNetCore" }.Any(u => category.StartsWith(u)) && logLevel >= LogLevel.Information;
        });

        // 配置接口超时时间和请求大小（Kestrel方式）
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
            options.Limits.MaxRequestBodySize = 1073741824; // 限制大小1GB（默认28.6MB）
        });
        // 配置 Formoptions（multipart/form-data）请求大小
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 1073741824; // 限制大小1GB（默认128MB）
        });
    }
}

public class ApplicationComponent : IApplicationComponent
{
    /// <summary>
    /// 构建 WebApplication 对象过程中装载中间件
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <param name="componentContext"></param>
    public void Load(IApplicationBuilder app, IWebHostEnvironment env, ComponentContext componentContext)
    {
        // 扫描所有继承 AppStartup 的类（排序执行顺序）
        var startups = App.EffectiveTypes
            .Where(u => typeof(AppStartup).IsAssignableFrom(u) && u.IsClass && !u.IsAbstract && !u.IsGenericType && u.GetMethod("LoadAppComponent") != null)
            .OrderByDescending(u => !u.IsDefined(typeof(AppStartupAttribute), true) ? 0 : u.GetCustomAttribute<AppStartupAttribute>(true).Order);
        if (startups == null || !startups.Any())
            return;

        try
        {
            foreach (var type in startups)
            {
                var startup = Activator.CreateInstance(type) as AppStartup;
                var initDataMethod = type.GetMethod("LoadAppComponent");
                initDataMethod?.Invoke(startup, [app, env, componentContext]);
            }
        }
        catch { }
    }
}