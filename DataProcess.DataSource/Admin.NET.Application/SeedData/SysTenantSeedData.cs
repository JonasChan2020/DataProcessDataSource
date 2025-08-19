// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Application;

/// <summary>
/// 系统租户表种子数据（执行顺序大于0且Id与框架一致，则代表重写系统种子数据）
/// </summary>
[SeedData(500)]
public class SysTenantSeedData : ISqlSugarEntitySeedData<SysTenant>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysTenant> HasData()
    {
        var defaultDbConfig = App.GetOptions<DbConnectionOptions>().ConnectionConfigs[0];
        var userAdmin = new SysUserSeedData().HasData().ToList().First(u => u.Account == "admin");

        return
        [
            new SysTenant
            {
                Id=SqlSugarConst.DefaultTenantId,
                OrgId=SqlSugarConst.DefaultTenantId,
                UserId=userAdmin.Id,
                Host="gitee.com",
                TenantType=TenantTypeEnum.Id,
                DbType=defaultDbConfig.DbType,
                Connection=defaultDbConfig.ConnectionString,
                ConfigId=SqlSugarConst.MainConfigId,
                Remark="默认租户",
                CreateTime=DateTime.Parse("2022-02-10 00:00:00"),
                Logo="/upload/logo.png",
                Title="Admin.NET.Pro",
                ViceTitle="Admin.NET",
                ViceDesc="站在巨人肩膀上的 .NET 通用权限开发框架",
                Copyright="Copyright © 2021-present Admin.NET All rights reserved.",
                Icp="省ICP备12345678号",
                IcpUrl="https://beian.miit.gov.cn",
                Watermark="Admin.NET",
                Version="v2.0.0",
                ThemeColor="#0f59a4",
                Layout="columns", // defaults|classic|transverse|columns
                Animation="fadeDown",
                Captcha=true,
                SecondVer=false
            },
        ];
    }
}