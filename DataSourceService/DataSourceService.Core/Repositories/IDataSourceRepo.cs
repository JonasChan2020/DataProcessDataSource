namespace DataSourceService.Core.Repositories;

using DataSourceService.Core.Entities;

public interface IDataSourceRepo
{
    Task<List<DataSource>> GetListAsync(string? typeCode);
    Task<DataSource?> GetAsync(string code);
    Task InsertAsync(DataSource entity);
    Task UpdateAsync(DataSource entity);
    Task DeleteAsync(string code);
}
