using Furion.DynamicApiController;
using Furion.FriendlyException;              // Oops
using Microsoft.Extensions.Logging;          // ILogger<T>
using SqlSugar;
using DataProcess.DataSource.Application.Dto;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// 多数据源事务示例服务
/// </summary>
[ApiDescriptionSettings(Order = 200, Name = "多数据源事务")]
public class DataSourceMultiDbService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;                       // 统一用 ISqlSugarClient
    private readonly ILogger<DataSourceMultiDbService> _logger;

    public DataSourceMultiDbService(ISqlSugarClient db,
                                    ILogger<DataSourceMultiDbService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 跨库事务示例（mainDb 和 logDb）
    /// </summary>
    public async Task<ApiResponse<bool>> MultiDbTranDemo()
    {
        var tenant = _db.AsTenant();

        var result = await tenant.UseTranAsync(async () =>
        {
            var mainDb = tenant.GetConnectionScope("mainDb");
            var logDb = tenant.GetConnectionScope("logDb");

            await mainDb.Insertable(new TestTable { Name = "主库数据" }).ExecuteCommandAsync();
            await logDb.Insertable(new TestLog { Log = "日志库数据" }).ExecuteCommandAsync();
            // UseTranAsync 的 lambda 不要 return 值
        },
        ex => _logger.LogError(ex, "跨库事务失败"));

        if (!result.IsSuccess)
            throw Oops.Oh("多库事务失败：" + result.ErrorMessage);

        return ApiResponse<bool>.Success(true);
    }
}
