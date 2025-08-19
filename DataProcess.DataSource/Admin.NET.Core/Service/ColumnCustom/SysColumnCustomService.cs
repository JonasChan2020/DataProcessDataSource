// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 表格列配置服务 🧩
/// </summary>
/// <param name="rep"></param>
/// <param name="um"></param>
/// <param name="cache"></param>
[ApiDescriptionSettings(Order = 245, Description = "代码生成模板配置")]
public class SysColumnCustomService(SqlSugarRepository<SysColumnCustom> rep, UserManager um, SysCacheService cache) : IDynamicApiController, ITransient
{
    /// <summary>
    /// 获取用户表格列配置信息 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [SuppressMonitor]
    [DisplayName("获取用户表格列配置信息")]
    public async Task<ColumnCustomOutput> GetDetail([FromQuery] GetColumnCustomInput input)
    {
        var key = $"{CacheConst.KeyColumnCustom}{um.UserId}:{input.GridId}";
        var result = cache.Get<ColumnCustomOutput>(key);
        if (result is null)
        {
            var temp = await rep.GetFirstAsync(e => e.UserId == um.UserId && e.GridId == input.GridId);
            if (temp != null)
            {
                result = new ColumnCustomOutput
                {
                    UserId = temp.UserId,
                    GridId = temp.GridId,
                    FixedData = string.IsNullOrEmpty(temp.FixedData) ? null : JSON.Deserialize<Dictionary<string, string>>(temp.FixedData),
                    ResizableData = string.IsNullOrEmpty(temp.ResizableData) ? null : JSON.Deserialize<Dictionary<string, int>>(temp.ResizableData),
                    SortData = string.IsNullOrEmpty(temp.SortData) ? null : JSON.Deserialize<Dictionary<string, int>>(temp.SortData),
                    VisibleData = string.IsNullOrEmpty(temp.VisibleData) ? null : JSON.Deserialize<Dictionary<string, bool>>(temp.VisibleData),
                };
                cache.Set(key, result, TimeSpan.FromDays(7));
            }
        }
        return result;
    }

    /// <summary>
    /// 保存用户表格列配置信息 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("保存用户表格列配置信息")]
    public async Task Store(StoreColumnCustomInput input)
    {
        var temp = await rep.GetFirstAsync(e => e.UserId == um.UserId && e.GridId == input.GridId);
        if (temp is null) temp = new SysColumnCustom { UserId = um.UserId, GridId = input.GridId };
        else cache.Remove($"{CacheConst.KeyColumnCustom}{um.UserId}:{input.GridId}");  // 移除缓存
        temp.FixedData = JSON.Serialize(input.FixedData);
        temp.ResizableData = JSON.Serialize(input.ResizableData);
        temp.SortData = JSON.Serialize(input.SortData);
        temp.VisibleData = JSON.Serialize(input.VisibleData);
        await rep.Context.Storageable(temp).ExecuteCommandAsync();
    }

    /// <summary>
    /// 清除用户表格列配置信息 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("清除用户表格列配置信息")]
    public async Task Reset(ResetColumnCustomInput input)
    {
        await rep.AsDeleteable().Where(e => e.UserId == um.UserId && e.GridId == input.GridId).ExecuteCommandAsync();
        cache.Remove($"{CacheConst.KeyColumnCustom}{um.UserId}:{input.GridId}");  // 移除缓存
    }
}