namespace DataSourceService.Core.Repositories;

using DataSourceService.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDataSourceTypeRepo
{
    Task<List<DataSourceType>> GetListAsync();
    Task<DataSourceType?> GetAsync(string code);
    Task InsertAsync(DataSourceType entity);
    Task UpdateAsync(DataSourceType entity);
    Task DeleteAsync(string code);
}
