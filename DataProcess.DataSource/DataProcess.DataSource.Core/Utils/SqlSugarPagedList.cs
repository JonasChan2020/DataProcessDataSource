using SqlSugar;

namespace DataProcess.DataSource.Core.Paging;

public class SqlSugarPagedList<TEntity>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<TEntity> Items { get; set; } = Array.Empty<TEntity>();
    public bool HasPrevPage { get; set; }
    public bool HasNextPage { get; set; }
}

public static class SqlSugarPagedExtensions
{
    public static async Task<SqlSugarPagedList<TEntity>> ToPagedListAsync<TEntity>(this ISugarQueryable<TEntity> query, int pageIndex, int pageSize)
    {
        RefAsync<int> total = 0;
        var items = await query.ToPageListAsync(pageIndex, pageSize, total);
        return Create(items, total, pageIndex, pageSize);
    }

    private static SqlSugarPagedList<T> Create<T>(IEnumerable<T> items, int total, int pageIndex, int pageSize)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0;
        return new SqlSugarPagedList<T>
        {
            Page = pageIndex,
            PageSize = pageSize,
            Items = items,
            Total = total,
            TotalPages = totalPages,
            HasNextPage = pageIndex < totalPages,
            HasPrevPage = pageIndex > 1
        };
    }
}