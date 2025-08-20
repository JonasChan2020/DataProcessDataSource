using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using DataProcess.DataSource.Application.Entity;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// ����Դ���񣨶�̬ API��
/// </summary>
[ApiDescriptionSettings(Order = 10, Description = "����Դ")]
public class DataSourceService : IDynamicApiController, ITransient
{
    private readonly IServiceProvider _services;

    public DataSourceService(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// ��ȡ����Դ�����б������ݿⲻ�����򷵻�����Ĭ��ֵ��
    /// GET /api/datasource/types
    /// </summary>
    [HttpGet("types")]
    [DisplayName("��ȡ����Դ�����б�")]
    public async Task<List<DataSourceType>> Types()
    {
        var db = _services.GetService<ISqlSugarClient>();
        if (db != null)
        {
            try
            {
                db.CodeFirst.InitTables<DataSourceType, DataSourceInstance>();

                var list = await db.Queryable<DataSourceType>().OrderBy(t => t.Name).ToListAsync();
                if (list.Count > 0) return list;

                var builtins = GetBuiltInTypes();
                if (builtins.Count > 0)
                {
                    await db.Insertable(builtins).ExecuteCommandAsync();
                    return builtins;
                }
            }
            catch
            {
                // �������ݿ��쳣�������ö���
            }
        }

        return GetBuiltInTypes();
    }

    private static List<DataSourceType> GetBuiltInTypes()
    {
        return new List<DataSourceType>
        {
            new DataSourceType
            {
                Id = 1,
                Code = "SqlServer",
                Name = "SqlServer",
                Description = "����SqlServer����Դ",
                Version = "1.0",
                AdapterClassName = "DataProcess.DataSource.Adapter.SqlServer.SqlServerAdapter",
                AssemblyName = "DataProcess.DataSource.Adapter.SqlServer",
                ParamTemplate = "{\"Server\":\"\",\"Database\":\"\",\"UserId\":\"\",\"Password\":\"\"}",
                Icon = "",
                IsBuiltIn = true,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            },
            new DataSourceType
            {
                Id = 2,
                Code = "MySql",
                Name = "MySQL",
                Description = "����MySQL����Դ",
                Version = "1.0",
                AdapterClassName = "DataProcess.DataSource.Adapter.MySql.MySqlAdapter",
                AssemblyName = "DataProcess.DataSource.Adapter.MySql",
                ParamTemplate = "{\"Server\":\"\",\"Database\":\"\",\"UserId\":\"\",\"Password\":\"\"}",
                Icon = "",
                IsBuiltIn = true,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }
        };
    }
}