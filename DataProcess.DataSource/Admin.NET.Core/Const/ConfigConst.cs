// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 配置常量
/// </summary>
public class ConfigConst
{
    /// <summary>
    /// 系统版本code
    /// </summary>
    public const string SysVersion = "sys_version";

    /// <summary>
    /// 当前系统版本值，标准版本号为 x.xx.xxxxxx。比如 1.01.250409，也可以是变体，比如 1.2.5 => v1.2.5
    /// </summary>
    public const string SysCurrentVersion = "1.01.000001";

    /// <summary>
    /// 演示环境
    /// </summary>
    public const string SysDemoEnv = "sys_demo";

    /// <summary>
    /// 默认密码
    /// </summary>
    public const string SysPassword = "sys_password";

    /// <summary>
    /// 密码最大错误次数
    /// </summary>
    public const string SysPasswordMaxErrorTimes = "sys_password_max_error_times";

    /// <summary>
    /// 日志保留天数
    /// </summary>
    public const string SysLogRetentionDays = "sys_log_retention_days";

    /// <summary>
    /// 记录操作日志
    /// </summary>
    public const string SysOpLog = "sys_oplog";

    /// <summary>
    /// 单设备登录
    /// </summary>
    public const string SysSingleLogin = "sys_single_login";

    ///// <summary>
    ///// 登录二次验证
    ///// </summary>
    //public const string SysSecondVer = "sys_second_ver";

    ///// <summary>
    ///// 图形验证码
    ///// </summary>
    //public const string SysCaptcha = "sys_captcha";

    /// <summary>
    /// Token过期时间
    /// </summary>
    public const string SysTokenExpire = "sys_token_expire";

    /// <summary>
    /// RefreshToken过期时间
    /// </summary>
    public const string SysRefreshTokenExpire = "sys_refresh_token_expire";

    /// <summary>
    /// 发送异常日志邮件
    /// </summary>
    public const string SysErrorMail = "sys_error_mail";

    /// <summary>
    /// 域登录验证
    /// </summary>
    public const string SysDomainLogin = "sys_domain_login";

    ///// <summary>
    ///// 租户域名隔离登录验证
    ///// </summary>
    //public const string SysTenantHostLogin = "sys_tenant_host_login";

    ///// <summary>
    ///// 行政区划同步层级 1-省级,2-市级,3-区县级,4-街道级,5-村级
    ///// </summary>
    //public const string SysRegionSyncLevel = "sys_region_sync_level";

    /// <summary>
    /// 开启强制修改密码
    /// </summary>
    public const string SysForceChangePassword = "sys_force_change_password";

    /// <summary>
    /// 国密SM2密匙
    /// </summary>
    public const string SysSM2Key = "sys_sm2_key";

    /// <summary>
    /// 开启密码强度验证
    /// </summary>
    public const string SysPasswordStrength = "sys_password_strength";

    /// <summary>
    /// 密码强度验证正则表达式
    /// </summary>
    public const string SysPasswordStrengthExpression = "sys_password_strength_expression";

    /// <summary>
    /// 密码有效期验证
    /// </summary>
    public const string SysPasswordExpirationTime = "sys_password_expiration_time";

    /// <summary>
    /// 密码历史记录验证
    /// </summary>
    public const string SysPasswordRecord = "sys_password_record";

    /// <summary>
    /// 显示系统更新日志
    /// </summary>
    public const string SysUpgrade = "sys_upgrade";

    /// <summary>
    /// 开启多语言切换
    /// </summary>
    public const string SysI18NSwitch = "sys_i18n_switch";

    /// <summary>
    /// 闲置超时时间
    /// </summary>
    public const string SysIdleTimeout = "sys_idle_timeout";

    /// <summary>
    /// 上线通知
    /// </summary>
    public const string SysOnlineNotice = "sys_online_notice";

    /// <summary>
    /// 支付宝授权页面地址
    /// </summary>
    public const string AlipayAuthPageUrl = "alipay_auth_page_url_";

    /// <summary>
    /// Default 分组
    /// </summary>
    public const string SysDefaultGroup = "Default";

    /// <summary>
    /// 系统图标
    /// </summary>
    public const string SysWebLogo = "sys_web_logo";

    /// <summary>
    /// 系统主标题
    /// </summary>
    public const string SysWebTitle = "sys_web_title";

    /// <summary>
    /// 系统副标题
    /// </summary>
    public const string SysWebViceTitle = "sys_web_viceTitle";

    /// <summary>
    /// 系统描述
    /// </summary>
    public const string SysWebViceDesc = "sys_web_viceDesc";

    /// <summary>
    /// 水印内容
    /// </summary>
    public const string SysWebWatermark = "sys_web_watermark";

    /// <summary>
    /// 版权说明
    /// </summary>
    public const string SysWebCopyright = "sys_web_copyright";

    /// <summary>
    /// ICP备案号
    /// </summary>
    public const string SysWebIcp = "sys_web_icp";

    /// <summary>
    /// ICP地址
    /// </summary>
    public const string SysWebIcpUrl = "sys_web_icpUrl";
}