// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Hardware.Info;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统通用服务 🧩
/// </summary>
[ApiDescriptionSettings(Order = 101, Description = "通用接口")]
[AllowAnonymous]
public class SysCommonService : IDynamicApiController, ITransient
{
    private readonly IApiDescriptionGroupCollectionProvider _apiProvider;
    private readonly SysCacheService _sysCacheService;
    private readonly IHttpRemoteService _httpRemoteService;

    public SysCommonService(IApiDescriptionGroupCollectionProvider apiProvider, SysCacheService sysCacheService, IHttpRemoteService httpRemoteService)
    {
        _apiProvider = apiProvider;
        _sysCacheService = sysCacheService;
        _httpRemoteService = httpRemoteService;
    }

    /// <summary>
    /// 获取国密公钥私钥对 🏆
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取国密公钥私钥对")]
    public SmKeyPairOutput GetSmKeyPair()
    {
        return CryptogramHelper.GetSmKeyPair();
    }

    /// <summary>
    /// 获取MD5加密字符串 🏆
    /// </summary>
    /// <param name="text"></param>
    /// <param name="uppercase"></param>
    /// <returns></returns>
    [DisplayName("获取MD5加密字符串")]
    public string GetMD5Encrypt(string text, bool uppercase = false)
    {
        return MD5Encryption.Encrypt(text, uppercase, is16: false);
    }

    /// <summary>
    /// 国密SM2加密字符串 🔖
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    [DisplayName("国密SM2加密字符串")]
    public string SM2Encrypt([Required] string plainText)
    {
        return CryptogramHelper.SM2Encrypt(plainText);
    }

    /// <summary>
    /// 国密SM2解密字符串 🔖
    /// </summary>
    /// <param name="cipherText"></param>
    /// <returns></returns>
    [DisplayName("国密SM2解密字符串")]
    public string SM2Decrypt([Required] string cipherText)
    {
        return CryptogramHelper.SM2Decrypt(cipherText);
    }

    /// <summary>
    /// 获取所有接口/动态API 🔖
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="isAppApi"></param>
    /// <returns></returns>
    [DisplayName("获取所有接口/动态API")]
    public List<ApiOutput> GetApiList([FromQuery] string groupName = "", [FromQuery] bool isAppApi = false)
    {
        var apiList = new List<ApiOutput>();

        //// 路由前缀
        //var defaultRoutePrefix = App.GetOptions<DynamicApiControllerSettingsOptions>().DefaultRoutePrefix;

        //var menuIdList = _userManager.SuperAdmin ? new List<long>() : await GetMenuIdList();

        // 获取所有接口分组
        var apiDescriptionGroups = _apiProvider.ApiDescriptionGroups.Items;
        foreach (ApiDescriptionGroup group in apiDescriptionGroups)
        {
            if (!string.IsNullOrWhiteSpace(groupName) && group.GroupName != groupName)
                continue;

            var apiOuput = new ApiOutput
            {
                Name = "",
                Text = string.IsNullOrWhiteSpace(group.GroupName) ? "系统接口" : group.GroupName,
                Route = "",
            };
            // 获取分组的所有接口
            var actions = group.Items;
            foreach (ApiDescription action in actions)
            {
                // 路由
                var route = action.RelativePath.Contains('{') ? action.RelativePath[..(action.RelativePath.IndexOf('{') - 1)] : action.RelativePath; // 去掉路由参数
                route = route[(route.IndexOf('/') + 1)..]; // 去掉路由前缀

                // 接口分组/控制器信息
                if (action.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
                    continue;

                // 是否只获取所有的移动端/AppApi接口
                if (isAppApi)
                {
                    var appApiDescription = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute<AppApiDescriptionAttribute>(true);
                    if (appApiDescription == null) continue;
                }

                var apiDescription = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute<ApiDescriptionSettingsAttribute>(true);
                var controllerName = controllerActionDescriptor.ControllerName;
                var actionName = controllerActionDescriptor.ActionName;
                var controllerText = apiDescription?.Description;
                if (!apiOuput.Children.Exists(u => u.Name == controllerName))
                {
                    apiOuput.Children.Add(new ApiOutput
                    {
                        Name = controllerName,
                        Text = string.IsNullOrWhiteSpace(controllerText) ? controllerName : controllerText,
                        Route = "",
                        Order = apiDescription?.Order ?? 0,
                    });
                }

                // 接口信息
                var apiController = apiOuput.Children.FirstOrDefault(u => u.Name.Equals(controllerName));
                apiDescription = controllerActionDescriptor.MethodInfo.GetCustomAttribute<ApiDescriptionSettingsAttribute>(true);
                var apiText = apiDescription?.Description;
                if (string.IsNullOrWhiteSpace(apiText))
                    apiText = controllerActionDescriptor.MethodInfo.GetCustomAttribute<DisplayNameAttribute>(true)?.DisplayName;
                apiController.Children.Add(new ApiOutput
                {
                    Name = "",
                    Text = apiText,
                    Route = route,
                    Action = actionName,
                    HttpMethod = action.HttpMethod,
                    Order = apiDescription?.Order ?? 0,
                });

                // 接口分组/控制器排序
                apiOuput.Children = [.. apiOuput.Children.OrderByDescending(u => u.Order)];
            }

            apiList.Add(apiOuput);
        }
        return apiList;
    }

    /// <summary>
    /// 获取所有移动端接口 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取所有移动端接口")]
    public List<string> GetAppApiList()
    {
        var apiList = _sysCacheService.Get<List<string>>(CacheConst.KeyAppApi);
        if (apiList == null)
        {
            apiList = [];
            var allApiList = GetApiList("", true);
            foreach (var apiOutput in allApiList)
            {
                foreach (var controller in apiOutput.Children)
                    apiList.AddRange(controller.Children.Select(u => u.Route));
            }
            _sysCacheService.Set(CacheConst.KeyAppApi, apiList, TimeSpan.FromDays(7));
        }
        return apiList;
    }

    /// <summary>
    /// 生成所有移动端接口文件 🔖
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="isAppApi"></param>
    [HttpGet]
    [DisplayName("生成所有移动端接口文件")]
    public void GenerateAppApi([FromQuery] string groupName = "", [FromQuery] bool isAppApi = true)
    {
        var defaultRoutePrefix = App.GetOptions<DynamicApiControllerSettingsOptions>().DefaultRoutePrefix;
        var apiPath = Path.Combine(App.WebHostEnvironment.ContentRootPath, @"App\api");

        var allApiList = GetApiList("", false); // 此处暂时获取全部
        foreach (var apiOutput in allApiList)
        {
            foreach (var controller in apiOutput.Children)
            {
                // 以controller.Name为控制器名称，创建js文件.js
                var controllerName = controller.Name;
                var filePath = Path.Combine(apiPath, $"{controllerName}.js");
                StringBuilder stringBuilder = new();
                stringBuilder.Append(@"import { http } from 'uview-plus'");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
                foreach (var item in controller.Children)
                {
                    var value = item.HttpMethod.Equals("get", StringComparison.CurrentCultureIgnoreCase) ? "params" : "data";

                    stringBuilder.Append($@"// {item.Text}");
                    stringBuilder.AppendLine();
                    stringBuilder.Append($@"export const {item.Action}Api = ({value}) => http.{item.HttpMethod.ToLower()}('/{defaultRoutePrefix}/{item.Route}', {value})");
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine();
                }
                // 如果或文件夹文件不存在则创建，存在则覆盖
                if (!Directory.Exists(apiPath))
                    Directory.CreateDirectory(apiPath);
                File.WriteAllText(filePath, stringBuilder.ToString());
            }
        }
    }

    /// <summary>
    /// 下载标记错误的临时 Excel（全局） 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("下载标记错误的临时 Excel")]
    public async Task<IActionResult> DownloadErrorExcelTemp([FromQuery] string fileName = null)
    {
        var userId = App.User?.FindFirst(ClaimConst.UserId)?.Value;
        var resultStream = _sysCacheService.Get<MemoryStream>(CacheConst.KeyExcelTemp + userId) ?? throw Oops.Oh("错误标记文件已过期。");

        return await Task.FromResult(new FileStreamResult(resultStream, "application/octet-stream")
        {
            FileDownloadName = $"{(string.IsNullOrEmpty(fileName) ? "错误标记＿" + DateTime.Now.ToString("yyyyMMddhhmmss") : fileName)}.xlsx"
        });
    }

    /// <summary>
    /// 获取机器序列号 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取机器序列号")]
    public string GetMachineSerialKey()
    {
        try
        {
            HardwareInfo hardwareInfo = new();
            hardwareInfo.RefreshBIOSList(); // 刷新 BIOS 信息
            hardwareInfo.RefreshMotherboardList(); // 刷新主板信息
            hardwareInfo.RefreshCPUList(false); // 刷新 CPU 信息

            var biosSerialNumber = hardwareInfo.BiosList.MinBy(u => u.SerialNumber)?.SerialNumber;
            var mbSerialNumber = hardwareInfo.MotherboardList.MinBy(u => u.SerialNumber)?.SerialNumber;
            var cpuProcessorId = hardwareInfo.CpuList.MinBy(u => u.ProcessorId)?.ProcessorId;
            // 根据 BIOS、主板和 CPU 信息生成 MD5 摘要
            var md5Data = MD5Encryption.Encrypt($"{biosSerialNumber}_{mbSerialNumber}_{cpuProcessorId}", true);
            var serialKey = $"{md5Data[..8]}-{md5Data[8..16]}-{md5Data[16..24]}-{md5Data[24..]}";
            return serialKey;
        }
        catch (Exception ex)
        {
            throw Oops.Oh($"获取机器码失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 性能压力测试 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("性能压力测试")]
    public async Task<StressTestHarnessResult> StressTest(StressTestInput input)
    {
        var stressTestHarnessResult = await _httpRemoteService.SendAsync(HttpRequestBuilder.StressTestHarness(input.RequestUri)
            .SetNumberOfRequests(input.NumberOfRequests) // 并发请求数量
            .SetNumberOfRounds(input.NumberOfRounds) // 压测轮次
            .SetMaxDegreeOfParallelism(input.MaxDegreeOfParallelism) // 最大并发度
            .WithRequest(builder => builder.WithHeaders(input.Headers)
                            .WithQueryParameters(input.QueryParameters)
                            .WithPathParameters(input.PathParameters)
                            .SetJsonContent(input.JsonContent)));
        return stressTestHarnessResult;
    }
}