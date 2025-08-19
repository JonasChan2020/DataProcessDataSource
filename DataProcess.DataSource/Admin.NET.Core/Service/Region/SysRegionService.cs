// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Furion.Shapeless;

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统行政区划服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 310, Description = "行政区划")]
public class SysRegionService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysRegion> _sysRegionRep;
    private readonly IHttpRemoteService _httpRemoteService;

    public SysRegionService(SqlSugarRepository<SysRegion> sysRegionRep, IHttpRemoteService httpRemoteService)
    {
        _sysRegionRep = sysRegionRep;
        _httpRemoteService = httpRemoteService;
    }

    /// <summary>
    /// 获取行政区划分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取行政区划分页列表")]
    public async Task<SqlSugarPagedList<SysRegion>> Page(PageRegionInput input)
    {
        return await _sysRegionRep.AsQueryable()
            .WhereIF(input.Pid > 0, u => u.Pid == input.Pid || u.Id == input.Pid)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), u => u.Code.Contains(input.Code))
            .OrderBy(u => new { u.Code })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取行政区划列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取行政区划列表")]
    public async Task<List<SysRegion>> GetList([FromQuery] RegionInput input)
    {
        return await _sysRegionRep.GetListAsync(u => u.Pid == input.Id);
    }

    /// <summary>
    /// 获取指定层级行政区划子树 🔖
    /// </summary>
    /// <param name="pid"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    [DisplayName("获取指定层级行政区划子树")]
    public async Task<List<SysRegion>> GetChildTree(long pid, int level)
    {
        var iSugarQueryable = _sysRegionRep.AsQueryable().OrderBy(u => new { u.Code });
        return await iSugarQueryable.Where(u => u.Level < level).ToTreeAsync(u => u.Children, u => u.Pid, pid);
    }

    /// <summary>
    /// 获取指定层级行政区划子列表 🔖
    /// </summary>
    /// <param name="pid"></param>
    /// <returns></returns>
    [DisplayName("获取指定层级行政区划子列表")]
    public async Task<List<SysRegion>> GetChildList(long pid)
    {
        return await _sysRegionRep.AsQueryable().Where(u => u.Pid == pid).OrderBy(u => new { u.Code }).ToListAsync();
    }

    /// <summary>
    /// 查询行政区划列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Query"), HttpPost]
    [DisplayName("查询行政区划列表")]
    public async Task<List<SysRegion>> QueryList(QueryRegionInput input)
    {
        return await _sysRegionRep.AsQueryable()
            .WhereIF(input.Pid.HasValue, u => u.Pid == input.Pid)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Type), u => u.Type == input.Type)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Code), u => u.Code.Contains(input.Code))
            .OrderBy(u => new { u.Code })
            .ToListAsync();
    }

    /// <summary>
    /// 增加行政区划 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加行政区划")]
    public async Task<long> AddRegion(AddRegionInput input)
    {
        input.Code = input.Code?.Trim() ?? "";
        if (input.Code.Length != 12 && input.Code.Length != 9 && input.Code.Length != 6) throw Oops.Oh(ErrorCodeEnum.R2003);

        if (input.Pid != 0)
        {
            var pRegion = await _sysRegionRep.GetByIdAsync(input.Pid);
            pRegion ??= await _sysRegionRep.GetFirstAsync(u => u.Code == input.Pid.ToString());
            if (pRegion == null) throw Oops.Oh(ErrorCodeEnum.D2000);
            input.Pid = pRegion.Id;
        }

        var isExist = await _sysRegionRep.IsAnyAsync(u => u.Name == input.Name && u.Code == input.Code);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.R2002);

        var sysRegion = input.Adapt<SysRegion>();
        var newRegion = await _sysRegionRep.AsInsertable(sysRegion).ExecuteReturnEntityAsync();
        return newRegion.Id;
    }

    /// <summary>
    /// 更新行政区划 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新行政区划")]
    public async Task UpdateRegion(UpdateRegionInput input)
    {
        input.Code = input.Code?.Trim() ?? "";
        if (input.Code.Length != 12 && input.Code.Length != 9 && input.Code.Length != 6) throw Oops.Oh(ErrorCodeEnum.R2003);

        var sysRegion = await _sysRegionRep.GetByIdAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);

        if (sysRegion.Pid != input.Pid && input.Pid != 0)
        {
            var pRegion = await _sysRegionRep.GetByIdAsync(input.Pid);
            pRegion ??= await _sysRegionRep.GetFirstAsync(u => u.Code == input.Pid.ToString());
            if (pRegion == null) throw Oops.Oh(ErrorCodeEnum.D2000);

            input.Pid = pRegion.Id;
            var regionTreeList = await _sysRegionRep.AsQueryable().ToChildListAsync(u => u.Pid, input.Id, true);
            var childIdList = regionTreeList.Select(u => u.Id).ToList();
            if (childIdList.Contains(input.Pid)) throw Oops.Oh(ErrorCodeEnum.R2004);
        }

        if (input.Id == input.Pid) throw Oops.Oh(ErrorCodeEnum.R2001);

        var isExist = await _sysRegionRep.IsAnyAsync(u => (u.Name == input.Name && u.Code == input.Code) && u.Id != sysRegion.Id);
        if (isExist) throw Oops.Oh(ErrorCodeEnum.R2002);

        //// 父Id不能为自己的子节点
        //var regionTreeList = await _sysRegionRep.AsQueryable().ToChildListAsync(u => u.Pid, input.Id, true);
        //var childIdList = regionTreeList.Select(u => u.Id).ToList();
        //if (childIdList.Contains(input.Pid))
        //    throw Oops.Oh(ErrorCodeEnum.R2001);

        await _sysRegionRep.AsUpdateable(input.Adapt<SysRegion>()).IgnoreColumns(true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除行政区划 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除行政区划")]
    public async Task DeleteRegion(DeleteRegionInput input)
    {
        var regionTreeList = await _sysRegionRep.AsQueryable().ToChildListAsync(u => u.Pid, input.Id, true);
        var regionIdList = regionTreeList.Select(u => u.Id).ToList();
        await _sysRegionRep.DeleteAsync(u => regionIdList.Contains(u.Id));
    }

    /// <summary>
    /// 同步行政区划（民政部） 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("同步行政区划（民政部）")]
    public async Task SyncRegionMzb(MzbInput input)
    {
        try
        {
            var html = await _httpRemoteService.GetAsStringAsync("http://xzqh.mca.gov.cn/map");
            var municipalityList = new List<string> { "北京", "天津", "上海", "重庆" };

            var proJson = Regex.Match(html, @"(?<=var json = )(\[\{.*?\}\])(?=;)").Value;
            dynamic provList = Clay.Parse(proJson);

            var list = new List<SysRegion>();
            foreach (var proItem in provList)
            {
                var provName = proItem.shengji;
                var province = new SysRegion
                {
                    Id = YitIdHelper.NextId(),
                    Name = Regex.Replace(provName, "[(（].*?[）)]", ""),
                    Code = proItem.quHuaDaiMa,
                    CityCode = proItem.quhao,
                    Level = 1,
                    Pid = 0,
                };
                //if (municipalityList.Any(u => province.Name.StartsWith(u))) province.Name += "(省)";
                list.Add(province);

                if (input.Level <= 1) continue;

                var cityList = await GetSelectList(provName);
                foreach (var cityItem in cityList)
                {
                    var cityName = cityItem.diji;
                    var city = new SysRegion
                    {
                        Id = YitIdHelper.NextId(),
                        Code = cityItem.quHuaDaiMa,
                        CityCode = cityItem.quhao,
                        Pid = province.Id,
                        Name = cityName,
                        Level = 2
                    };
                    if (municipalityList.Any(u => city.Name.StartsWith(u)))
                    {
                        city.Name = "市辖区";
                        if (province.Code == city.Code) city.Code = province.Code.Substring(0, 2) + "0100";
                    }
                    list.Add(city);

                    if (input.Level <= 2) continue;

                    var countyList = await GetSelectList(provName, cityName);
                    foreach (var countyItem in countyList)
                    {
                        var countyName = countyItem.xianji;
                        var county = new SysRegion
                        {
                            Id = YitIdHelper.NextId(),
                            Code = countyItem.quHuaDaiMa,
                            CityCode = countyItem.quhao,
                            Name = countyName,
                            Pid = city.Id,
                            Level = 3
                        };
                        if (city.Code.IsNullOrEmpty())
                        {
                            // 省直辖县级行政单位 节点无Code编码处理
                            city.Code = county.Code.Substring(0, 3).PadRight(6, '0');
                        }
                        list.Add(county);
                    }
                }
            }

            if (list.Count > 0)
            {
                await _sysRegionRep.AsDeleteable().ExecuteCommandAsync();
                await _sysRegionRep.Context.Fastest<SysRegion>().BulkCopyAsync(list);
            }
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ex);
        }

        // 获取选择数据
        async Task<Clay> GetSelectList(string prov, string prefecture = null)
        {
            var json = await _httpRemoteService.PostAsStringAsync("http://xzqh.mca.gov.cn/selectJson", builder => builder.SetJsonContent(new
            {
                shengji = prov,
                diji = prefecture,
            }));
            return Clay.Parse(json);
        }
    }

    /// <summary>
    /// 同步行政区划（高德） 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("同步行政区划（高德）")]
    public async Task SyncRegionGD(GDInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Key) || input.Key.Length < 30)
            throw Oops.Oh("请正确输入高德地图开发者 Key 值");

        var res = await _httpRemoteService.GetAsync($"https://restapi.amap.com/v3/config/district?keywords={input.Keywords}&subdistrict={input.Level}&key={input.Key}");
        if (!res.IsSuccessStatusCode) return;

        var gdResponse = JSON.Deserialize<GDResponse<List<GDRegionResponse>>>(res.Content.ReadAsStringAsync().Result);
        if (gdResponse.info != "OK" || gdResponse.districts == null || gdResponse.districts.Count < 1) return;

        var regionList = new List<SysRegion>();
        foreach (var item in gdResponse.districts)
        {
            GetChildren(regionList, item.districts, 1, 0); // 排除一级目录（国家）
        }

        await _sysRegionRep.AsDeleteable().ExecuteCommandAsync();
        await _sysRegionRep.Context.Fastest<SysRegion>().BulkCopyAsync(regionList);
    }

    private static void GetChildren(List<SysRegion> regionList, List<GDRegionResponse> responses, int level, long pid)
    {
        foreach (var region in responses)
        {
            var sysRegion = new SysRegion { Id = YitIdHelper.NextId(), Pid = pid, Name = region.name, Code = region.adcode, CityCode = region.adcode, Level = level };
            regionList.Add(sysRegion);

            if (region.districts.Count > 0)
                GetChildren(regionList, region.districts, level++, sysRegion.Id);
        }
    }

    /// <summary>
    /// 同步行政区划数据（国家地名信息库，最多支持2级深度） 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("同步行政区划数据（国家地名信息库）")]
    public async Task<int> SyncRegionMca(McaInput input)
    {
        var url = $"https://dmfw.mca.gov.cn/9095/xzqh/getList?code={input.Code}&maxLevel={input.Level}";

        var res = await _httpRemoteService.GetAsStreamAsync(url);
        SysRegion regionLevel0 = ((dynamic)Clay.Parse(res)).data;
        if (regionLevel0 == null) return 0;

        var areaList = new List<SysRegion>();
        if (regionLevel0.Code != "00" && regionLevel0.Level > 0 && !string.IsNullOrEmpty(regionLevel0.Name))
        {
            areaList.Add(new SysRegion
            {
                Id = Convert.ToInt64(regionLevel0.Code),
                Pid = 0,
                Code = regionLevel0.Code,
                Name = regionLevel0.Name,
                Type = regionLevel0.Type,
                Level = regionLevel0.Level,
            });
        }
        if (regionLevel0.Children != null)
        {
            foreach (var regionLevel1 in regionLevel0.Children)
            {
                var region1 = new SysRegion
                {
                    Id = Convert.ToInt64(regionLevel1.Code),
                    Pid = Convert.ToInt64(regionLevel0.Code),
                    Code = regionLevel1.Code,
                    Name = regionLevel1.Name,
                    Type = regionLevel1.Type,
                    Level = regionLevel1.Level,
                };
                if (areaList.Any(u => u.Id == region1.Id))
                    Console.WriteLine($"1 级：{region1.Id} - {region1.Name} 已存在");
                else
                    areaList.Add(region1);
                if (regionLevel1.Children == null) continue;
                foreach (var regionLevel2 in regionLevel1.Children)
                {
                    var region2 = new SysRegion
                    {
                        Id = Convert.ToInt64(regionLevel2.Code),
                        Pid = Convert.ToInt64(regionLevel1.Code),
                        Code = regionLevel2.Code,
                        Name = regionLevel2.Name,
                        Type = regionLevel2.Type,
                        Level = regionLevel2.Level,
                    };
                    if (areaList.Any(u => u.Id == region2.Id))
                        Console.WriteLine($"2 级：{region2.Id} - {region2.Name} 已存在");
                    else
                        areaList.Add(region2);
                    if (regionLevel2.Children == null) continue;
                    foreach (var regionLevel3 in regionLevel2.Children)
                    {
                        var region3 = new SysRegion
                        {
                            Id = Convert.ToInt64(regionLevel3.Code),
                            Pid = Convert.ToInt64(regionLevel2.Code),
                            Code = regionLevel3.Code,
                            Name = regionLevel3.Name,
                            Type = regionLevel3.Type,
                            Level = regionLevel3.Level,
                        };
                        if (areaList.Any(u => u.Id == region3.Id))
                            Console.WriteLine($"3 级：{region3.Id} - {region3.Name}");
                        else
                            areaList.Add(region3);
                        if (regionLevel3.Children == null) continue;
                        foreach (var regionLevel4 in regionLevel3.Children)
                        {
                            var region4 = new SysRegion
                            {
                                Id = Convert.ToInt64(regionLevel4.Code),
                                Pid = Convert.ToInt64(regionLevel3.Code),
                                Code = regionLevel4.Code,
                                Name = regionLevel4.Name,
                                Type = regionLevel4.Type,
                                Level = regionLevel4.Level,
                            };
                            areaList.Add(region4);
                        }
                    }
                }
            }
        }

        if (input.Code == 0)
            await _sysRegionRep.AsDeleteable().ExecuteCommandAsync();
        else if (await _sysRegionRep.IsAnyAsync(u => u.Id == input.Code)) // 如果存在指定行政区划则删除
            await DeleteRegion(new DeleteRegionInput { Id = input.Code });
        return await _sysRegionRep.AsInsertable(areaList).ExecuteCommandAsync();
    }

    /// <summary>
    /// 同步行政区划数据（天地图行政区划） 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("同步行政区划数据（天地图行政区划）")]
    public async Task<int> SyncRegionTianditu(TiandituInput input)
    {
        // 接口说明及地址：http://lbs.tianditu.gov.cn/server/administrative2.html
        var url = $"http://api.tianditu.gov.cn/v2/administrative?keyword={input.Keyword}&childLevel={input.ChildLevel}&extensions={input.Extensions}&tk={input.Tk}";

        var res = await _httpRemoteService.GetAsAsync<TiandituDto>(url);
        if (res == null || res.District == null) return 0;

        var parent = res.District[0];
        var areaList = new List<SysRegion>()
        {
            new()
            {
                Id = Convert.ToInt64(parent.Gb),
                Pid = 0,
                Code = parent.Gb,
                Name = parent.Name,
                Level = parent.Level,
                Longitude = parent.Center.Lng,
                Latitude = parent.Center.Lat
            }
        };

        foreach (var item in parent.Children)
        {
            var region = new SysRegion
            {
                Id = Convert.ToInt64(item.Gb),
                Pid = Convert.ToInt64(parent.Gb),
                Code = item.Gb,
                Name = item.Name,
                Level = item.Level,
                Longitude = item.Center.Lng,
                Latitude = item.Center.Lat
            };
            areaList.Add(region);

            foreach (var child in item.Children)
            {
                areaList.Add(new SysRegion
                {
                    Id = Convert.ToInt64(child.Gb),
                    Pid = region.Id,
                    Code = child.Gb,
                    Name = child.Name,
                    Level = child.Level,
                    Longitude = child.Center.Lng,
                    Latitude = child.Center.Lat
                });
            }
        }

        // 若存在指定行政区划则删除
        if (await _sysRegionRep.IsAnyAsync(u => u.Name.Contains(input.Keyword) || u.Id.ToString() == input.Keyword))
        {
            var region = await _sysRegionRep.GetFirstAsync(u => u.Name.Contains(input.Keyword) || u.Id.ToString() == input.Keyword);
            await DeleteRegion(new DeleteRegionInput { Id = region.Id });
        }

        return await _sysRegionRep.AsInsertable(areaList).ExecuteCommandAsync();
    }

    /// <summary>
    /// 生成组织架构 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("生成组织架构")]
    public async Task GenOrg(GenOrgInput input)
    {
        var region = await _sysRegionRep.GetByIdAsync(input.Id);
        var orgRep = _sysRegionRep.ChangeRepository<SqlSugarRepository<SysOrg>>();
        if (!await orgRep.IsAnyAsync(u => u.Id == region.Pid))
            region.Pid = 0;

        var regionList = await GetRegionListByLevel(region, input.Level);
        var orgList = regionList.Adapt<List<SysOrg>>();
        await orgRep.InsertOrUpdateAsync(orgList);
    }

    /// <summary>
    /// 根据层级获取行政区划数据
    /// </summary>
    /// <param name="region"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    private async Task<List<SysRegion>> GetRegionListByLevel(SysRegion region, int level)
    {
        var regionList = new List<SysRegion>();
        if (level > 5) level = 5;
        regionList.Add(region);

        if (level == 1) return regionList;
        var regionList2 = await GetList(new RegionInput { Id = region.Id });
        regionList.AddRange(regionList2);

        if (level == 2) return regionList;
        foreach (var item in regionList2)
        {
            var regionList3 = await GetList(new RegionInput { Id = item.Id });
            if (regionList3 == null) continue;
            regionList.AddRange(regionList3);

            if (level == 3) continue;
            foreach (var item3 in regionList3)
            {
                var regionList4 = await GetList(new RegionInput { Id = item3.Id });
                if (regionList4 == null) continue;
                regionList.AddRange(regionList4);

                if (level == 4) continue;
                foreach (var item4 in regionList4)
                {
                    var regionList5 = await GetList(new RegionInput { Id = item4.Id });
                    if (regionList5 == null) continue;
                    regionList.AddRange(regionList5);
                }
            }
        }
        return regionList;
    }

    /// <summary>
    /// 从 china.sqlite 中获取区划数据
    /// </summary>
    /// <param name="code">区划编码</param>
    /// <param name="level">级数（从当前code所在级别往下级数）</param>
    /// <returns></returns>
    [DisplayName("从 china.sqlite 中获取区划数据")]
    public async Task GetRegionTree(string code, int level)
    {
        level = level > 5 ? 5 : level;

        var sqlitePath = "C:\\china.sqlite";
        var db = new SqlSugarScope(new ConnectionConfig()
        {
            ConnectionString = $"Data Source={sqlitePath};Cache=Shared",
            DbType = SqlSugar.DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        var regionList = new List<SysRegion>();

        // 判断编码所属层级
        int startLevel = 1; // 省
        switch (code.Length)
        {
            case 4:
                startLevel = 2; // 市
                break;

            case 6:
                startLevel = 3; // 区县
                break;

            case 9:
                startLevel = 4; // 街道
                break;

            case 12:
                startLevel = 5; // 社区/村
                break;

            default:
                break;
        }
        var region1List = GetRegionList(code, startLevel, db);
        if (region1List.Count == 0)
            return;
        region1List.ForEach(u => u.Pid = 0);
        regionList.AddRange(region1List);

        if (level == 1 || startLevel == 5)
            goto result;
        startLevel++;

        var region2List = new List<SysRegion>();
        foreach (var item in region1List)
        {
            region2List.AddRange(GetRegionList(item.Code, startLevel, db));
        }
        regionList.AddRange(region2List);

        if (level == 2 || startLevel == 5 || region2List.Count == 0)
            goto result;
        startLevel++;

        var region3List = new List<SysRegion>();
        foreach (var item in region2List)
        {
            region3List.AddRange(GetRegionList(item.Code, startLevel, db));
        }
        regionList.AddRange(region3List);

        if (level == 3 || startLevel == 5 || region3List.Count == 0)
            goto result;
        startLevel++;

        var region4List = new List<SysRegion>();
        foreach (var item in region3List)
        {
            region4List.AddRange(GetRegionList(item.Code, startLevel, db));
        }
        regionList.AddRange(region4List);

        if (level == 4 || startLevel == 5 || region4List.Count == 0)
            goto result;
        startLevel++;

        var region5List = new List<SysRegion>();
        region5List.AddRange(GetVillageList(region4List.Select(u => u.Code).ToList(), db));
        regionList.AddRange(region5List);

    result:

        // 保存行政区划树
        var defaultDbConfig = App.GetOptions<DbConnectionOptions>().ConnectionConfigs[0];
        db = new SqlSugarScope(new ConnectionConfig()
        {
            ConnectionString = defaultDbConfig.ConnectionString,
            DbType = defaultDbConfig.DbType,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
        await db.Deleteable<SysRegion>().ExecuteCommandAsync();
        await db.Insertable(regionList).ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据层级及父级编码获取区域集合
    /// </summary>
    /// <param name="pCode"></param>
    /// <param name="level"></param>
    /// <param name="db"></param>
    /// <returns></returns>
    private static List<SysRegion> GetRegionList(string pCode, int level, SqlSugarScope db)
    {
        string table = "";
        switch (level)
        {
            case 1:
                table = "province";
                break;

            case 2:
                table = "city";
                break;

            case 3:
                table = "area";
                break;

            case 4:
                table = "street";
                break;

            case 5:
                table = "village";
                break;

            default:
                break;
        }
        if (string.IsNullOrWhiteSpace(table))
            return [];

        var condition = string.IsNullOrWhiteSpace(pCode) || pCode == "0" ? "" : $" and code like '{pCode}%'";
        var sql = $"select * from {table} where 1=1 {condition}";
        var regions = db.Ado.SqlQuery<SysRegion>(sql);
        if (regions.Count == 0)
            return [];

        foreach (var item in regions)
        {
            item.Pid = string.IsNullOrWhiteSpace(pCode) || item.Code == pCode || level == 1 ? 0 : Convert.ToInt64(pCode);
            item.Level = level;
            item.Id = Convert.ToInt64(item.Code);
        }

        return regions;
    }

    /// <summary>
    /// 获取社区/村集合
    /// </summary>
    /// <param name="pCodes"></param>
    /// <param name="db"></param>
    /// <returns></returns>
    private static List<SysRegion> GetVillageList(List<string> pCodes, SqlSugarScope db)
    {
        var condition = pCodes == null || pCodes.Count == 0 ? "" : $" and streetCode in ('{pCodes.Join("','")}')";
        var sql = $"select * from village where 1=1 {condition}";
        var regions = db.Ado.SqlQuery<dynamic>(sql);
        if (regions.Count == 0)
            return [];

        var regionList = new List<SysRegion>();
        foreach (var item in regions)
        {
            var region = new SysRegion
            {
                Name = item.name,
                Code = item.code,
                Pid = Convert.ToInt64(item.streetCode),
                Level = 5,
                Id = Convert.ToInt64(item.code)
            };
            regionList.Add(region);
        }
        return regionList;
    }
}