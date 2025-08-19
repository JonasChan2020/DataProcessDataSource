namespace DataSourceService.Core.Repositories;

using DataSourceService.Core.Entities;
using Furion.DependencyInjection;
using DataSourceService.Core;
using System.Threading.Tasks;
using System.Collections.Generic;

public class DataSourceTypeRepo : IDataSourceTypeRepo, ITransient
{
    public async Task<List<DataSourceType>> GetListAsync()
    {
        return await DbContext.Instance.Queryable<DataSourceType>().ToListAsync();
    }

    public async Task<DataSourceType?> GetAsync(string code)
    {
        return await DbContext.Instance.Queryable<DataSourceType>().FirstAsync(x => x.Code == code);
    }

    public async Task InsertAsync(DataSourceType entity)
    {
        await DbContext.Instance.Insertable(entity).ExecuteCommandAsync();
    }

    public async Task UpdateAsync(DataSourceType entity)
    {
        await DbContext.Instance.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(string code)
    {
        await DbContext.Instance.Deleteable<DataSourceType>().In(code).ExecuteCommandAsync();
    }
}
