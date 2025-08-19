// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Admin.NET.Application;

[AppStartup(100)]
public class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    /// <summary>
    /// 初始化与升级数据，在初始化表结构之前调用
    /// </summary>
    /// <param name="dbProvider"></param>
    /// <param name="oldVerion">旧版本，0表示从来没有设置过，-1表示当前连接不是主数据库，从SysConfig表获取的</param>
    /// <param name="currentVersion">当前版本，由ConfigConst.SysCurrentVersion转换的整数版本</param>
    public void BeforeInitTable(SqlSugarScopeProvider dbProvider, long oldVerion, long currentVersion)
    {
        // 比较版本号对数据库进行升级结构、种子数据等
    }

    /// <summary>
    /// 初始化与升级数据，在初始化种子数据之后调用
    /// </summary>
    /// <param name="dbProvider"></param>
    /// <param name="oldVerion">旧版本，0表示从来没有设置过，-1表示当前连接不是主数据库，从SysConfig表获取的</param>
    /// <param name="currentVersion">当前版本，由ConfigConst.SysCurrentVersion转换的整数版本</param>
    public void AfterInitSeed(SqlSugarScopeProvider dbProvider, long oldVerion, long currentVersion)
    {
        // 比较版本号对数据库进行升级结构、种子数据等
    }

    /// <summary>
    /// 构建 WebApplication 对象过程中装载中间件
    /// </summary>
    /// <param name="application">WebApplication对象</param>
    /// <param name="env"></param>
    /// <param name="componentContext"></param>
    public void LoadAppComponent(object application, IWebHostEnvironment env, ComponentContext componentContext)
    {
        WebApplication webApplication = application as WebApplication;
    }
}

/// <summary>
/// 测试InitDatas，Order越大，InitDatas执行越靠前
/// </summary>
[AppStartup(1000)]
public class TestStartup : AppStartup
{
    /// <summary>
    /// 初始化与升级数据，在初始化表结构之前调用
    /// </summary>
    /// <param name="dbProvider"></param>
    /// <param name="oldVerion">旧版本，0表示从来没有设置过，-1表示当前连接不是主数据库，从SysConfig表获取的</param>
    /// <param name="currentVersion">当前版本，由ConfigConst.SysCurrentVersion转换的整数版本</param>
    public void BeforeInitTable(SqlSugarScopeProvider dbProvider, long oldVerion, long currentVersion)
    {
        // 比较版本号对数据库进行升级结构、种子数据等
    }

    /// <summary>
    /// 初始化与升级数据，在初始化种子数据之后调用
    /// </summary>
    /// <param name="dbProvider"></param>
    /// <param name="oldVerion">旧版本，0表示从来没有设置过，-1表示当前连接不是主数据库，从SysConfig表获取的</param>
    /// <param name="currentVersion">当前版本，由ConfigConst.SysCurrentVersion转换的整数版本</param>
    public void AfterInitSeed(SqlSugarScopeProvider dbProvider, long oldVerion, long currentVersion)
    {
        // 比较版本号对数据库进行升级结构、种子数据等
    }
}