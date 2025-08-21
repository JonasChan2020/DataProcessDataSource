using Furion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using System.Collections.Generic;
using System.Text.Json;
// 显式加载动态 API 所在程序集
using DataProcess.DataSource.Application.Service;
using System.IO;
using System;

namespace DataProcess.DataSource.Web.Core
{
    [AppStartup(int.MaxValue)]
    public class Startup : AppStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // 读取 DbConnection（优先配置中心，其次回退到运行目录 Configuration/Database.json）
            services.AddSingleton<ISqlSugarClient>(_ =>
            {
                var raw = App.GetConfig<List<SimpleConn>>("DbConnection:ConnectionConfigs") ?? new();
                if (raw.Count == 0)
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "Configuration", "Database.json");
                    if (File.Exists(path))
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(path));
                        if (doc.RootElement.TryGetProperty("DbConnection", out var dbNode) &&
                            dbNode.TryGetProperty("ConnectionConfigs", out var ccNode) &&
                            ccNode.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in ccNode.EnumerateArray())
                            {
                                raw.Add(new SimpleConn
                                {
                                    ConfigId = el.TryGetProperty("ConfigId", out var v1) ? v1.GetString() : "MainDb",
                                    DbType = el.TryGetProperty("DbType", out var v2) ? v2.GetString() : "SqlServer",
                                    ConnectionString = el.TryGetProperty("ConnectionString", out var v3) ? v3.GetString() : "",
                                    IsAutoCloseConnection = el.TryGetProperty("IsAutoCloseConnection", out var v4) ? v4.GetBoolean() : true
                                });
                            }
                        }
                    }
                }

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

            // 开启动态 API + 显式加入 Application 程序集
            var mvc = services.AddControllers().AddInjectWithUnifyResult();
            mvc.AddApplicationPart(typeof(DataSourceTypeService).Assembly);
            services.AddDynamicApiControllers(); // 关键：启用 Furion 动态 API

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
                "kdbndp" or "kingbase" => DbType.Kdbndp,
                "dm" or "dameng" => DbType.Dm,
                "oscar" or "shentong" => DbType.Oscar,
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
