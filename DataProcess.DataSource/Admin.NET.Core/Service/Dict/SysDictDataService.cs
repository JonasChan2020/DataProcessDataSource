// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统字典值服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 420, Description = "系统字典值")]
public class SysDictDataService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysDictData> _sysDictDataRep;
    private readonly SysCacheService _sysCacheService;
    private readonly UserManager _userManager;

    public SysDictDataService(SqlSugarRepository<SysDictData> sysDictDataRep,
        SysCacheService sysCacheService,
        UserManager userManager)
    {
        _sysDictDataRep = sysDictDataRep;
        _sysCacheService = sysCacheService;
        _userManager = userManager;
    }

    /// <summary>
    /// 获取字典值分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取字典值分页列表")]
    public async Task<SqlSugarPagedList<SysDictData>> Page(PageDictDataInput input)
    {
        return await _sysDictDataRep.AsQueryable()
            .Where(u => u.DictTypeId == input.DictTypeId)
            .WhereIF(!string.IsNullOrEmpty(input.Code?.Trim()), u => u.Code.Contains(input.Code))
            .WhereIF(!string.IsNullOrEmpty(input.Label?.Trim()), u => u.Label.Contains(input.Label))
            .OrderBy(u => new { u.OrderNo, u.Code })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取字典值列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取字典值列表")]
    public async Task<List<SysDictData>> GetList([FromQuery] GetDataDictDataInput input)
    {
        return await GetDictDataListByDictTypeId(input.DictTypeId);
    }

    /// <summary>
    /// 增加字典值 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加字典值")]
    public async Task AddDictData(AddDictDataInput input)
    {
        var isExist = await _sysDictDataRep.IsAnyAsync(u => u.Code == input.Code && u.DictTypeId == input.DictTypeId);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D3003);

        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(input.DictTypeId);
        if (dictType.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3008);

        _sysCacheService.Remove($"{CacheConst.KeyDict}{dictType.Code}");

        var dictData = dictType.IsTenant == YesNoEnum.Y ? input.Adapt<SysDictDataTenant>() : input.Adapt<SysDictData>();
        await _sysDictDataRep.Context.Insertable(dictData).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新字典值 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新字典值")]
    public async Task UpdateDictData(UpdateDictDataInput input)
    {
        var isExist = await _sysDictDataRep.IsAnyAsync(u => u.Id == input.Id);
        if (!isExist) throw Oops.Oh(ErrorCodeEnum.D3004);

        isExist = await _sysDictDataRep.IsAnyAsync(u => u.Code == input.Code && u.DictTypeId == input.DictTypeId && u.Id != input.Id);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.D3003);

        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(input.DictTypeId);
        if (dictType.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3009);

        _sysCacheService.Remove($"{CacheConst.KeyDict}{dictType.Code}");
        var dictData = dictType.IsTenant == YesNoEnum.Y ? input.Adapt<SysDictDataTenant>() : input.Adapt<SysDictData>();
        await _sysDictDataRep.Context.Updateable(dictData).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除字典值 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除字典值")]
    public async Task DeleteDictData(DeleteDictDataInput input)
    {
        var dictData = await _sysDictDataRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3004);

        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(dictData.DictTypeId);
        if (dictType.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3010);

        _sysCacheService.Remove($"{CacheConst.KeyDict}{dictType.Code}");
        var entity = dictType.IsTenant == YesNoEnum.Y ? input.Adapt<SysDictDataTenant>() : input.Adapt<SysDictData>();
        await _sysDictDataRep.Context.Deleteable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取字典值详情 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取字典值详情")]
    public async Task<SysDictData> GetDetail([FromQuery] DictDataInput input)
    {
        return await _sysDictDataRep.GetByIdAsync(input.Id);
    }

    /// <summary>
    /// 修改字典值状态 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [UnitOfWork]
    [DisplayName("修改字典值状态")]
    public async Task SetStatus(DictDataInput input)
    {
        var dictData = await _sysDictDataRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D3004);

        var dictType = await _sysDictDataRep.Change<SysDictType>().GetByIdAsync(dictData.DictTypeId);
        if (dictType.SysFlag == YesNoEnum.Y && !_userManager.SuperAdmin) throw Oops.Oh(ErrorCodeEnum.D3009);

        _sysCacheService.Remove($"{CacheConst.KeyDict}{dictType.Code}");

        dictData.Status = input.Status;
        var entity = dictType.IsTenant == YesNoEnum.Y ? input.Adapt<SysDictDataTenant>() : input.Adapt<SysDictData>();
        await _sysDictDataRep.Context.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据字典类型Id获取字典值集合
    /// </summary>
    /// <param name="dictTypeId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<SysDictData>> GetDictDataListByDictTypeId(long dictTypeId)
    {
        return await GetDataListByIdOrCode(dictTypeId, null);
    }

    /// <summary>
    /// 根据字典类型编码获取字典值集合 🔖
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    [DisplayName("根据字典类型编码获取字典值集合")]
    public async Task<List<SysDictData>> GetDataList(string code)
    {
        return await GetDataListByIdOrCode(null, code);
    }

    /// <summary>
    /// 获取字典值集合 🔖
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<SysDictData>> GetDataListByIdOrCode(long? typeId, string code)
    {
        if (string.IsNullOrWhiteSpace(code) && typeId == null ||
            !string.IsNullOrWhiteSpace(code) && typeId != null)
            throw Oops.Oh(ErrorCodeEnum.D3011);

        var dictDataList = _sysCacheService.Get<List<SysDictData>>($"{CacheConst.KeyDict}{code}");
        if (dictDataList == null)
        {
            dictDataList = await _sysDictDataRep.Change<SysDictType>().AsQueryable()
                .LeftJoin<SysDictData>((u, a) => u.Id == a.DictTypeId).ClearFilter()
                .WhereIF(!string.IsNullOrWhiteSpace(code), u => u.Code == code)
                .WhereIF(typeId != null && typeId > 0, u => u.Id == typeId)
                .Where((u, a) => u.Status == StatusEnum.Enable && a.Status == StatusEnum.Enable)
                .OrderBy((u, a) => new { a.OrderNo, a.Code })
                .Select((u, a) => a).ToListAsync();
            _sysCacheService.Set($"{CacheConst.KeyDict}{code}", dictDataList);
        }
        return dictDataList;

        //// 判断字典是否存在
        //var dictType = await _sysDictDataRep.Change<SysDictType>().AsQueryable()
        //    .WhereIF(!string.IsNullOrWhiteSpace(code), u => u.Code == code)
        //    .WhereIF(typeId != null, u => u.Id == typeId)
        //    .FirstAsync();
        //if (dictType == null) return null;

        //var dictDataList = _sysCacheService.Get<List<SysDictData>>($"{CacheConst.KeyDict}{dictType.Code}");
        //if (dictDataList == null)
        //{
        //    dictDataList = await _sysDictDataRep.AsQueryable()
        //        .InnerJoin<SysDictType>((u, a) => u.DictTypeId == a.Id)
        //        .Where(u => u.DictTypeId == dictType.Id)
        //        .Select((u, a) => new SysDictData
        //        {
        //            Status = u.Status == StatusEnum.Enable && a.Status == StatusEnum.Enable ? StatusEnum.Enable : StatusEnum.Disable,
        //        }, true)
        //        .OrderBy(u => new { u.OrderNo, u.Code })
        //        .ToListAsync();
        //    _sysCacheService.Set($"{CacheConst.KeyDict}{dictType.Code}", dictDataList);
        //}
        //return dictDataList;
    }

    /// <summary>
    /// 根据查询条件获取字典值集合 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据查询条件获取字典值集合")]
    public async Task<List<SysDictData>> GetDataList([FromQuery] QueryDictDataInput input)
    {
        var dataList = await GetDataList(input.Value);
        if (input.Status.HasValue) return dataList.Where(u => u.Status == (StatusEnum)input.Status.Value).ToList();
        return dataList;
    }

    /// <summary>
    /// 根据字典类型Id删除字典值
    /// </summary>
    /// <param name="dictTypeId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task DeleteDictData(long dictTypeId)
    {
        var dictType = await _sysDictDataRep.Change<SysDictType>().AsQueryable().Where(u => u.Id == dictTypeId).FirstAsync();
        _sysCacheService.Remove($"{CacheConst.KeyDict}{dictType?.Code}");

        if (dictType?.IsTenant == YesNoEnum.Y)
            await _sysDictDataRep.Change<SysDictDataTenant>().DeleteAsync(u => u.DictTypeId == dictTypeId);
        else
            await _sysDictDataRep.DeleteAsync(u => u.DictTypeId == dictTypeId);
    }
}