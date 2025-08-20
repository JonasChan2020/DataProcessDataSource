using System.Collections.Generic;

namespace DataProcess.DataSource.Application.SeedData.Abstractions;

/// <summary>
/// ʵ���������ݽӿڣ�ģ�����Ը���
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface ISqlSugarEntitySeedData<TEntity> where TEntity : class, new()
{
    IEnumerable<TEntity> HasData();
}