// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统信息保存输入参数
/// </summary>
public class SysInfoInput
{
    /// <summary>
    /// 租户Id
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 系统图标
    /// </summary>
    public string Logo { get; set; }

    /// <summary>
    /// 主标题
    /// </summary>
    [Required(ErrorMessage = "主标题不能为空")]
    public string Title { get; set; }

    /// <summary>
    /// 副标题
    /// </summary>
    public string ViceTitle { get; set; }

    /// <summary>
    /// 副描述
    /// </summary>
    public string ViceDesc { get; set; }

    /// <summary>
    /// 版权信息
    /// </summary>
    [Required(ErrorMessage = "版权信息不能为空")]
    public string Copyright { get; set; }

    /// <summary>
    /// ICP备案号
    /// </summary>
    [Required(ErrorMessage = "ICP备案号不能为空")]
    public string Icp { get; set; }

    /// <summary>
    /// ICP地址
    /// </summary>
    [Required(ErrorMessage = "ICP地址不能为空")]
    public string IcpUrl { get; set; }

    /// <summary>
    /// 水印
    /// </summary>
    public string Watermark { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 主题颜色
    /// </summary>
    [Required(ErrorMessage = "主题颜色不能为空")]
    public string ThemeColor { get; set; }

    /// <summary>
    /// 布局模式
    /// </summary>
    [Required(ErrorMessage = "布局模式不能为空")]
    public string Layout { get; set; }

    /// <summary>
    /// 页面动画
    /// </summary>
    [Required(ErrorMessage = "页面动画不能为空")]
    public string Animation { get; set; }

    /// <summary>
    /// 图形验证码
    /// </summary>
    public bool Captcha { get; set; } = true;

    /// <summary>
    /// 登录二次验证
    /// </summary>
    public bool SecondVer { get; set; } = false;

    /// <summary>
    /// 轮播图Id集合
    /// </summary>
    public List<long> CarouselFileIds { get; set; }
}