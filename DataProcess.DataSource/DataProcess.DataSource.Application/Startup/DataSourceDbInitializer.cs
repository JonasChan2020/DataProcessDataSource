using DataProcess.DataSource.Core.Entity;
using SqlSugar;

namespace DataProcess.DataSource.Application.Startup;

/// <summary>
/// ����Դģ�����ݿ��ʼ��
/// </summary>
public class DataSourceDbInitializer : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            // �Զ�����
            db.CodeFirst.InitTables<DataSourceType, DataSourceInstance>();

            // ע���������ͣ���δ���ڣ�
            var builtInTypes = new[]
            {
                new DataSourceType
                {
                    Code = "SqlServer",
                    Name = "SqlServer",
                    Description = "����SqlServer����Դ",
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
                    Description = "����MySQL����Դ",
                    Version = "1.0",
                    AdapterClassName = "DataProcess.DataSource.Adapter.MySql.MySqlAdapter",
                    AssemblyName = "DataProcess.DataSource.Adapter.MySql",
                    ParamTemplate = "{\"Server\":\"\",\"Database\":\"\",\"UserId\":\"\",\"Password\":\"\"}",
                    Icon = "",
                    IsBuiltIn = true
                }
                // �ɼ�����չ������������
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