namespace DataSourceService.Core.Repositories;

using DataSourceService.Core.Entities;
using Furion.DependencyInjection;
using DataSourceService.Core;

public class DataSourceRepo : IDataSourceRepo, ITransient
{
    public async Task<List<DataSource>> GetListAsync(string? typeCode)
    {
        var query = DbContext.Instance.Queryable<DataSource>();
        if (!string.IsNullOrEmpty(typeCode))
        {
            query = query.Where(x => x.TypeCode == typeCode);
        }
        return await query.ToListAsync();
    }

    public async Task<DataSource?> GetAsync(string code)
    {
        return await DbContext.Instance.Queryable<DataSource>().FirstAsync(x => x.Code == code);
    }

    public async Task InsertAsync(DataSource entity)
    {
        await DbContext.Instance.Insertable(entity).ExecuteCommandAsync();
    }

    public async Task UpdateAsync(DataSource entity)
    {
        await DbContext.Instance.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(string code)
    {
        await DbContext.Instance.Deleteable<DataSource>().In(code).ExecuteCommandAsync();
    }
}
