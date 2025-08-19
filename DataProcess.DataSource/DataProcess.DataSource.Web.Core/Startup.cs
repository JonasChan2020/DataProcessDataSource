using Furion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using System.Collections.Generic;

namespace DataProcess.DataSource.Web.Core
{
    /// <summary>
    /// 启动配置，参考 Admin.NET.Web.Core/Startup.cs，注册 SqlSugar、Furion、统一异常、Swagger、日志等
    /// </summary>
    [AppStartup(int.MaxValue)]
    public class Startup : AppStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // 注册 SqlSugar（需实现类似 Admin.NET.Core/SqlSugar/SqlSugarSetup.cs 的 AddSqlSugar 扩展方法，或直接在此处实现初始化）
            services.AddSingleton<ISqlSugarClient>(provider =>
            {
                // 读取配置文件，初始化 SqlSugarScope
                var configs = App.GetConfig<List<ConnectionConfig>>("ConnectionConfigs");
                return new SqlSugarScope(configs, db => { });
            });

            // 注册 JWT（如有需要可自定义 JwtHandler）
            services.AddJwt<JwtHandler>();

            // 注册统一异常处理
            services.AddConsoleFormatter();

            // 注册 CORS
            services.AddCorsAccessor();

            // 注册 Furion 动态 WebAPI + 统一响应
            services.AddControllers()
                    .AddInjectWithUnifyResult();

            // 注册 Swagger/OpenAPI
            services.AddSpecificationDocuments();

            // 注册日志
            services.AddLogging();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCorsAccessor();

            app.UseAuthentication();
            app.UseAuthorization();

            // 启用 Swagger
            app.UseSpecificationDocuments();

            // 启用统一异常状态码拦截
            app.UseUnifyResultStatusCodes();

            // 启用 Furion 动态 WebAPI
            app.UseInject(string.Empty);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
