// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

public class PageRegionInput : BasePageInput
{
    /// <summary>
    /// 父节点Id
    /// </summary>
    public long Pid { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; }
}

public class RegionInput : BaseIdInput
{
}

public class QueryRegionInput
{
    /// <summary>
    /// 父节点Id
    /// </summary>
    public long? Pid { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public string Type { get; set; }
}

public class AddRegionInput : SysRegion
{
    /// <summary>
    /// 名称
    /// </summary>
    [Required(ErrorMessage = "名称不能为空")]
    public override string Name { get; set; }
}

public class UpdateRegionInput : AddRegionInput
{
}

public class DeleteRegionInput : BaseIdInput
{
}

public class SyncInput
{
    /// <summary>
    /// 指定省
    /// </summary>
    public string Province { get; set; }

    /// <summary>
    /// 指定市
    /// </summary>
    public string City { get; set; }
}

public class GenOrgInput
{
    /// <summary>
    /// 区域Id
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 生成层级
    /// </summary>
    public int Level { get; set; }
}

public class TiandituInput
{
    /// <summary>
    /// 规则：只支持单个关键词语搜索
    /// 关键词支持：行政区划名称、行政区划编码
    /// 例如，keyword='北京' 或 keyword = '156110000'
    /// 说明：仅行政区划名称支持模糊查询
    /// 注：keyword只有一个字符时，将只返回suggestion字段值，不返回district字段值
    /// </summary>
    public string Keyword { get; set; }

    /// <summary>
    /// 规则：设置显示下级行政区级数（行政区级别包括：国家、省/直辖市、市、区/县多级数据
    /// 可选值：0、1、2、3
    /// 0：不返回下级行政区
    /// 1：返回下一级行政区
    /// 2：返回下两级行政区
    /// 3：返回下三级行政区
    /// 需要在此特殊说明，目前部分城市和省直辖县因为没有区县的概念，故在省级下方直接显示区县。
    /// 例如：河南-济源
    /// </summary>
    public string ChildLevel { get; set; }

    /// <summary>
    /// 是否需要轮廓数据
    /// 可选值：true、false
    /// true：返回轮廓数据
    /// false：不返回轮廓数据
    /// </summary>
    public bool Extensions { get; set; } = false;

    /// <summary>
    /// 密钥
    /// </summary>
    public string Tk { get; set; }
}

public class TiandituDto
{
    public List<TiandituInfo> District { get; set; }
}

public class TiandituInfo
{
    /// <summary>
    /// 行政编码
    /// </summary>
    public string Gb { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 中心点
    /// </summary>
    public CenterPoint Center { get; set; }

    /// <summary>
    /// 级别
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 子项
    /// </summary>
    public List<TiandituInfo> Children { get; set; }
}

/// <summary>
/// 中心点经纬度
/// </summary>
public class CenterPoint
{
    /// <summary>
    /// 经度
    /// </summary>
    public decimal Lng { get; set; }

    /// <summary>
    /// 纬度
    /// </summary>
    public decimal Lat { get; set; }
}