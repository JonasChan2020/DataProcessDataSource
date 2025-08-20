using DataProcess.DataSource.Core.Entity;
using SqlSugar;

namespace DataProcess.DataSource.Application.Startup;

/// <summary>
/// 数据源模块数据库初始化
/// </summary>
public class DataSourceDbInitializer : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            // 自动建表
            db.CodeFirst.InitTables<DataSourceType, DataSourceInstance>();

            // 注入内置类型（如未存在）
            var builtInTypes = new[]
            {
                new DataSourceType
                {
                    Code = "SqlServer",
                    Name = "SqlServer",
                    Description = "内置SqlServer数据源",
                    Version = "1.0",
                    AdapterClassName = "DataProcess.DataSource.Adapter.SqlServer.SqlServerAdapter",
                    AssemblyName = "DataProcess.DataSource.Adapter.SqlServer",
                    ParamTemplate = "{\"Server\":\"\",\"Database\":\"\",\"UserId\":\"\",\"Password\":\"\"}",
                    Icon = "",
                    IsBuiltIn = true
                },
                new DataSourceType
                {
                    Code = "MySql",
                    Name = "MySQL",
                    Description = "内置MySQL数据源",
                    Version = "1.0",
                    AdapterClassName = "DataProcess.DataSource.Adapter.MySql.MySqlAdapter",
                    AssemblyName = "DataProcess.DataSource.Adapter.MySql",
                    ParamTemplate = "{\"Server\":\"\",\"Database\":\"\",\"UserId\":\"\",\"Password\":\"\"}",
                    Icon = "",
                    IsBuiltIn = true
                }
                // 可继续扩展其它内置类型
            };

            foreach (var t in builtInTypes)
            {
                if (!db.Queryable<DataSourceType>().Any(x => x.Code == t.Code))
                {
                    db.Insertable(t).ExecuteCommand();
                }
            }

            next(app);
        };
    }
}