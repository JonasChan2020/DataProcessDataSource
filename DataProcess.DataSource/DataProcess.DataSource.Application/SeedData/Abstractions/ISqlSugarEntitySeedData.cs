using System.Collections.Generic;

namespace DataProcess.DataSource.Application.SeedData.Abstractions;

/// <summary>
/// 实体种子数据接口（模块内自给）
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface ISqlSugarEntitySeedData<TEntity> where TEntity : class, new()
{
    IEnumerable<TEntity> HasData();
}