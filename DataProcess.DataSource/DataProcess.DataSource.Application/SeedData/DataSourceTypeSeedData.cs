using System;
using System.Collections.Generic;
using System.Linq;
using DataProcess.DataSource.Application.Entity;
using DataProcess.DataSource.Application.SeedData.Abstractions;
using Furion;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace DataProcess.DataSource.Application.SeedData;

/// <summary>
/// 数据源类型种子数据（对齐 Admin.NET Seed 风格，模块内自给）
/// </summary>
[IgnoreUpdateSeed]
public class DataSourceTypeSeedData : ISqlSugarEntitySeedData<DataSourceType>
{
    public IEnumerable<DataSourceType> HasData()
    {
        const string adapterClass = "DataProcess.DataSource.Application.Service.Adapter.SqlSugarDataSourceAdapter";
        var asmName = typeof(DataSourceTypeSeedData).Assembly.GetName().Name ?? "DataProcess.DataSource.Application";

        // 稳定主键，避免多次 Seed 重复
        const long baseId = 1800000200000;

        var order = 1;
        foreach (var v in Enum.GetValues(typeof(DbType)).Cast<DbType>())
        {
            var code = v.ToString();
            yield return new DataSourceType
            {
                Id = baseId + (long)v,
                Code = code,
                Name = code,
                Description = $"内置 {code} 数据源",
                Version = "1.0",
                AdapterClassName = adapterClass,
                AssemblyName = asmName,
                ParamTemplate = JSON.Serialize(new { ConnectionString = "", DbType = code }),
                Icon = "",
                IsBuiltIn = true,
                OrderNo = order++,
                Status = true
            };
        }
    }
}