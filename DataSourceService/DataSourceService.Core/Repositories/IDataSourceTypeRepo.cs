namespace DataSourceService.Core.Repositories;

using DataSourceService.Core.Entities;

public interface IDataSourceTypeRepo
{
    Task<List<DataSourceType>> GetListAsync();
    Task<DataSourceType?> GetAsync(string code);
    Task InsertAsync(DataSourceType entity);
    Task UpdateAsync(DataSourceType entity);
    Task DeleteAsync(string code);
}
