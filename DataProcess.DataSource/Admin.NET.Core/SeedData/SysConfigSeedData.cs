// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 系统配置表种子数据
/// </summary>
[IgnoreUpdateSeed]
public class SysConfigSeedData : ISqlSugarEntitySeedData<SysConfig>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysConfig> HasData()
    {
        return
        [
            new SysConfig{ Id=1300000000101, Name="演示环境", Code=ConfigConst.SysDemoEnv, Value="False", SysFlag=YesNoEnum.Y, Remark="演示环境", OrderNo=10, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000111, Name="默认密码", Code=ConfigConst.SysPassword, Value="Admin.NET++010101", SysFlag=YesNoEnum.Y, Remark="默认密码", OrderNo=20, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000121, Name="密码最大错误次数", Code=ConfigConst.SysPasswordMaxErrorTimes, Value="5", SysFlag=YesNoEnum.Y, Remark="允许密码最大输入错误次数", OrderNo=30, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000131, Name="日志保留天数", Code=ConfigConst.SysLogRetentionDays, Value="180", SysFlag=YesNoEnum.Y, Remark="日志保留天数（天）", OrderNo=40, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000141, Name="记录操作日志", Code=ConfigConst.SysOpLog, Value="True", SysFlag=YesNoEnum.Y, Remark="是否记录操作日志", OrderNo=50, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000151, Name="单设备登录", Code=ConfigConst.SysSingleLogin, Value="False", SysFlag=YesNoEnum.Y, Remark="是否开启单设备登录", OrderNo=60, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000161, Name="Token过期时间", Code=ConfigConst.SysTokenExpire, Value="60", SysFlag=YesNoEnum.Y, Remark="Token过期时间（分钟）", OrderNo=90, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000171, Name="RefreshToken过期时间", Code=ConfigConst.SysRefreshTokenExpire, Value="120", SysFlag=YesNoEnum.Y, Remark="刷新Token过期时间（分钟）（一般 refresh_token 的有效时间 > 2 * access_token 的有效时间）", OrderNo=100, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000181, Name="发送异常日志邮件", Code=ConfigConst.SysErrorMail, Value="False", SysFlag=YesNoEnum.Y, Remark="是否发送异常日志邮件", OrderNo=110, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000191, Name="域登录验证", Code=ConfigConst.SysDomainLogin, Value="False", SysFlag=YesNoEnum.Y, Remark="是否开启域登录验证", OrderNo=120, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            //new SysConfig{ Id=1300000000201, Name="行政区划同步层级", Code=ConfigConst.SysRegionSyncLevel, Value="3", SysFlag=YesNoEnum.Y, Remark="行政区划同步层级 1-省级,2-市级,3-区县级,4-街道级,5-村级", OrderNo=150, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            // 业务系统记得改密匙（修改密匙要重新生成数据库/账号表），通过接口(http://localhost:5005/api/sysCommon/smKeyPair)获取
            //new SysConfig{ Id=1300000000211, Name="国密SM2密匙", Code=ConfigConst.SysSM2Key, Value="04851D329AA3E38C2E7670AFE70E6E70E92F8769CA27C8766B12209A0FFBA4493B603EF7A0B9B1E16F0E8930C0406EA0B179B68DF28E25334BDEC4AE76D907E9E9;3A61D1D30C6302DABFF36201D936D0143EEF0C850AF28C5CA6D5C045AF8C5C8A", SysFlag=YesNoEnum.Y, Remark="国密SM2密匙", OrderNo=160, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2024-11-21 00:00:00") },
            new SysConfig{ Id=1300000000221, Name="开启强制修改密码", Code=ConfigConst.SysForceChangePassword, Value="False", SysFlag=YesNoEnum.Y, Remark="开启强制修改密码", OrderNo=170, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000231, Name="开启密码强度验证", Code=ConfigConst.SysPasswordStrength, Value="False", SysFlag=YesNoEnum.Y, Remark="开启密码强度验证", OrderNo=180, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2022-02-10 00:00:00") },
            new SysConfig{ Id=1300000000241, Name="密码强度验证正则表达式", Code=ConfigConst.SysPasswordStrengthExpression, Value="(?=^.{6,20}$)(?=.*\\d)(?=.*\\W+)(?=.*[A-Z])(?=.*[a-z])(?!.*\\n).*$", SysFlag=YesNoEnum.Y, Remark="必须包含大小写字母、数字和特殊字符的组合，长度在6-20之间", OrderNo=190, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2024-11-21 00:00:00") },
            new SysConfig{ Id=1300000000251, Name="密码时间有效期", Code=ConfigConst.SysPasswordExpirationTime, Value="0", SysFlag=YesNoEnum.Y, Remark="默认0表示永不过期，否则表示过期天数", OrderNo=200, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2024-12-17 00:00:00") },
            new SysConfig{ Id=1300000000261, Name="密码历史记录验证", Code=ConfigConst.SysPasswordRecord, Value="False", SysFlag=YesNoEnum.Y, Remark="是否验证历史密码禁止再次使用", OrderNo=210, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2024-12-17 00:00:00") },
            new SysConfig{ Id=1300000000271, Name="显示系统更新日志", Code=ConfigConst.SysUpgrade, Value="True", SysFlag=YesNoEnum.Y, Remark="是否显示系统更新日志", OrderNo=220, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2024-12-20 00:00:00") },
            new SysConfig{ Id=1300000000281, Name="多语言切换", Code=ConfigConst.SysI18NSwitch, Value="True", SysFlag=YesNoEnum.Y, Remark="是否显示多语言切换按钮", OrderNo=230, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2024-12-20 00:00:00") },
            new SysConfig{ Id=1300000000291, Name="闲置超时时间", Code=ConfigConst.SysIdleTimeout, Value="0", SysFlag=YesNoEnum.Y, Remark="闲置超时时间（秒），超时强制退出，0 表示不限制", OrderNo=240, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2024-12-20 00:00:00") },
            new SysConfig{ Id=1300000000301, Name="开启上线通知", Code=ConfigConst.SysOnlineNotice, Value="True", SysFlag=YesNoEnum.Y, Remark="开启用户上线、下线通知", OrderNo=250, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2025-06-06 00:00:00") },

            new SysConfig{ Id=1300000000999, Name="系统版本号", Code=ConfigConst.SysVersion, Value="0", SysFlag=YesNoEnum.Y, Remark= "系统版本号，用于自动升级，请勿手动填写", OrderNo=1000, GroupCode=ConfigConst.SysDefaultGroup, CreateTime=DateTime.Parse("2025-04-10 00:00:00") },
        ];
    }
}