// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 事件类型-系统用户操作枚举
/// </summary>
[Description("事件类型-系统用户操作枚举")]
public enum UserEventTypeEnum
{
    /// <summary>
    /// 增加用户
    /// </summary>
    [Description("增加用户")]
    Add = 0,

    /// <summary>
    /// 更新用户
    /// </summary>
    [Description("更新用户")]
    Update = 1,

    /// <summary>
    /// 删除用户
    /// </summary>
    [Description("删除用户")]
    Delete = 2,

    /// <summary>
    /// 授权用户角色
    /// </summary>
    [Description("授权用户角色")]
    UpdateRole = 3,

    /// <summary>
    /// 设置用户状态
    /// </summary>
    [Description("设置用户状态")]
    SetStatus = 4,

    /// <summary>
    /// 修改密码
    /// </summary>
    [Description("修改密码")]
    ChangePwd = 5,

    /// <summary>
    /// 重置密码
    /// </summary>
    [Description("重置密码")]
    ResetPwd = 6,

    /// <summary>
    /// 解除登录锁定
    /// </summary>
    [Description("解除登录锁定")]
    UnlockLogin = 7,

    /// <summary>
    /// 系统登录
    /// </summary>
    [Description("系统登录")]
    Login = 8,

    /// <summary>
    /// 系统退出
    /// </summary>
    [Description("系统退出")]
    Logout = 9,
}