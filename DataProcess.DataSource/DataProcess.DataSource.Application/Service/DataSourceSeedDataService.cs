using Furion.DynamicApiController;
using SqlSugar;
using DataProcess.DataSource.Application.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// 数据源种子数据服务
/// </summary>
[ApiDescriptionSettings(Order = 140, Name = "种子数据")]
public class DataSourceSeedDataService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;

    public DataSourceSeedDataService(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 初始化SqlSugar支持的所有数据源类型
    /// </summary>
    [HttpPost]
    public async Task InitBuiltInTypes()
    {
        var builtInTypes = new List<DataSourceType>
        {
            new() { Name = "SQL Server", Code = "SqlServer", Description = "Microsoft SQL Server数据库", IsBuiltIn = true, OrderNo = 1, Status = true, Icon = "mssql", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "MySQL", Code = "MySql", Description = "MySQL数据库", IsBuiltIn = true, OrderNo = 2, Status = true, Icon = "mysql", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "PostgreSQL", Code = "PostgreSQL", Description = "PostgreSQL数据库", IsBuiltIn = true, OrderNo = 3, Status = true, Icon = "postgresql", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "Oracle", Code = "Oracle", Description = "Oracle数据库", IsBuiltIn = true, OrderNo = 4, Status = true, Icon = "oracle", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "SQLite", Code = "Sqlite", Description = "SQLite数据库", IsBuiltIn = true, OrderNo = 5, Status = true, Icon = "sqlite", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "达梦", Code = "Dm", Description = "达梦数据库", IsBuiltIn = true, OrderNo = 6, Status = true, Icon = "dm", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "人大金仓", Code = "Kdbndp", Description = "人大金仓数据库", IsBuiltIn = true, OrderNo = 7, Status = true, Icon = "kingbase", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "神通", Code = "Oscar", Description = "神通数据库", IsBuiltIn = true, OrderNo = 8, Status = true, Icon = "oscar", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "TDengine", Code = "TDengine", Description = "TDengine时序数据库", IsBuiltIn = true, OrderNo = 9, Status = true, Icon = "tdengine", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "GaussDB", Code = "GaussDB", Description = "华为GaussDB数据库", IsBuiltIn = true, OrderNo = 10, Status = true, Icon = "gaussdb", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "OceanBase", Code = "OceanBaseForOracle", Description = "OceanBase数据库(Oracle模式)", IsBuiltIn = true, OrderNo = 11, Status = true, Icon = "oceanbase", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "Clickhouse", Code = "Clickhouse", Description = "Clickhouse数据库", IsBuiltIn = true, OrderNo = 12, Status = true, Icon = "clickhouse", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "QuestDB", Code = "QuestDB", Description = "QuestDB时序数据库", IsBuiltIn = true, OrderNo = 13, Status = true, Icon = "questdb", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "Doris", Code = "Doris", Description = "Apache Doris数据库", IsBuiltIn = true, OrderNo = 14, Status = true, Icon = "doris", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "Vastbase", Code = "Vastbase", Description = "海量数据Vastbase数据库", IsBuiltIn = true, OrderNo = 15, Status = true, Icon = "vastbase", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "Xugu", Code = "Xugu", Description = "虚谷数据库", IsBuiltIn = true, OrderNo = 16, Status = true, Icon = "xugu", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "Informix", Code = "Informix", Description = "Informix数据库", IsBuiltIn = true, OrderNo = 17, Status = true, Icon = "informix", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "Access", Code = "Access", Description = "Microsoft Access数据库", IsBuiltIn = true, OrderNo = 18, Status = true, Icon = "access", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "OpenGauss", Code = "OpenGauss", Description = "华为OpenGauss数据库", IsBuiltIn = true, OrderNo = 19, Status = true, Icon = "opengauss", Version = "1.0", CreateTime = DateTime.Now },
            new() { Name = "HighgoDB", Code = "HighgoDB", Description = "瀚高数据库", IsBuiltIn = true, OrderNo = 20, Status = true, Icon = "highgodb", Version = "1.0", CreateTime = DateTime.Now },
        };

        foreach (var type in builtInTypes)
        {
            var exist = await _db.Queryable<DataSourceType>()
                .Where(t => t.Code == type.Code)
                .AnyAsync();
                
            if (!exist)
            {
                await _db.Insertable(type).ExecuteCommandAsync();
            }
        }
    }

    /// <summary>
    /// 获取内置类型列表
    /// </summary>
    [HttpGet]
    public List<string> GetBuiltInTypeCodes()
    {
        return new List<string>
        {
            "SqlServer", "MySql", "PostgreSQL", "Oracle", "Sqlite", "Dm", "Kdbndp",
            "Oscar", "TDengine", "GaussDB", "OceanBaseForOracle", "Clickhouse",
            "QuestDB", "Doris", "Vastbase", "Xugu", "Informix", "Access",
            "OpenGauss", "HighgoDB"
        };
    }
}