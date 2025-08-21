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
    /// 启动配置（参考 Admin.NET.Web.Core）
    /// </summary>
    [AppStartup(int.MaxValue)]
    public class Startup : AppStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // 正确读取 Database.json: DbConnection:ConnectionConfigs
            services.AddSingleton<ISqlSugarClient>(_ =>
            {
                var raw = App.GetConfig<List<SimpleConn>>("DbConnection:ConnectionConfigs") ?? new();
                var configs = new List<ConnectionConfig>();
                foreach (var c in raw)
                {
                    var cfgId = string.IsNullOrWhiteSpace(c.ConfigId) ? "MainDb" : c.ConfigId!;
                    configs.Add(new ConnectionConfig
                    {
                        ConfigId = cfgId,
                        ConnectionString = c.ConnectionString ?? "",
                        DbType = ToDbType(c.DbType),
                        IsAutoCloseConnection = c.IsAutoCloseConnection ?? true,
                        InitKeyType = InitKeyType.Attribute,
                        MoreSettings = new ConnMoreSettings { IsAutoRemoveDataCache = true, PgSqlIsAutoToLower = false }
                    });
                }
                return new SqlSugarScope(configs, _ => { });
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

        private static DbType ToDbType(string? dbType)
        {
            if (string.IsNullOrWhiteSpace(dbType)) return DbType.SqlServer;
            return dbType.ToLower() switch
            {
                "mysql" or "mysqlconnector" => DbType.MySql,
                "sqlserver" => DbType.SqlServer,
                "postgresql" or "postgres" or "pg" => DbType.PostgreSQL,
                "sqlite" => DbType.Sqlite,
                "oracle" => DbType.Oracle,
                _ => DbType.SqlServer
            };
        }

        private class SimpleConn
        {
            public string? ConfigId { get; set; }
            public string? DbType { get; set; }
            public string? ConnectionString { get; set; }
            public bool? IsAutoCloseConnection { get; set; }
        }
    }
}