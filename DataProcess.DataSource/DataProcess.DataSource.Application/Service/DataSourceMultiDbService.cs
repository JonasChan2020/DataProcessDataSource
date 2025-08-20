using Furion.DynamicApiController;
using Furion.FriendlyException;              // Oops
using Microsoft.Extensions.Logging;          // ILogger<T>
using SqlSugar;
using DataProcess.DataSource.Application.Dto;
using DataProcess.DataSource.Application.Entity;

namespace DataProcess.DataSource.Application.Service;

/// <summary>
/// ������Դ����ʾ������
/// </summary>
[ApiDescriptionSettings(Order = 200, Name = "������Դ����")]
public class DataSourceMultiDbService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;                       // ͳһ�� ISqlSugarClient
    private readonly ILogger<DataSourceMultiDbService> _logger;

    public DataSourceMultiDbService(ISqlSugarClient db,
                                    ILogger<DataSourceMultiDbService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// �������ʾ����mainDb �� logDb��
    /// </summary>
    public async Task<ApiResponse<bool>> MultiDbTranDemo()
    {
        var tenant = _db.AsTenant();

        var result = await tenant.UseTranAsync(async () =>
        {
            var mainDb = tenant.GetConnectionScope("mainDb");
            var logDb = tenant.GetConnectionScope("logDb");

            await mainDb.Insertable(new TestTable { Name = "��������" }).ExecuteCommandAsync();
            await logDb.Insertable(new TestLog { Log = "��־������" }).ExecuteCommandAsync();
            // UseTranAsync �� lambda ��Ҫ return ֵ
        },
        ex => _logger.LogError(ex, "�������ʧ��"));

        if (!result.IsSuccess)
            throw Oops.Oh("�������ʧ�ܣ�" + result.ErrorMessage);

        return ApiResponse<bool>.Success(true);
    }
}
