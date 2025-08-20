using Furion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using System.Collections.Generic;
using System.Linq;

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
            // 注册 SqlSugar
            services.AddSingleton<ISqlSugarClient>(provider =>
            {
                var configs = App.GetConfig<List<ConnectionConfig>>("ConnectionConfigs") ?? new List<ConnectionConfig>();

                // 兜底为每个连接设置 ConfigId，避免后续通过 ConfigId 获取 Provider 失败
                foreach (var c in configs)
                {
                    if (c.ConfigId == null || string.IsNullOrWhiteSpace(c.ConfigId.ToString()))
                        c.ConfigId = "main";
                }

                return new SqlSugarScope(configs, db => { });
            });

            services.AddJwt<JwtHandler>();
            services.AddConsoleFormatter();
            services.AddCorsAccessor();
            services.AddControllers().AddInjectWithUnifyResult();
            services.AddSpecificationDocuments();
            services.AddLogging();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            else { app.UseExceptionHandler("/error"); app.UseHsts(); }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCorsAccessor();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSpecificationDocuments();
            app.UseUnifyResultStatusCodes();
            app.UseInject(string.Empty);
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
