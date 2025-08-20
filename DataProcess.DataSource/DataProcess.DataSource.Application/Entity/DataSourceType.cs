using SqlSugar;
using DataProcess.DataSource.Core;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// 数据源类型（业务实体，支持插件）
/// </summary>
[SugarTable("dp_ds_type", TableDescription = "数据源类型")]
public class DataSourceType : EntityBase
{
    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = default!;

    [SugarColumn(Length = 128, IsNullable = false)]
    public string Name { get; set; } = default!;

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(Length = 32, IsNullable = true)]
    public string? Version { get; set; }

    [SugarColumn(Length = 256, IsNullable = true)]
    public string? AdapterClassName { get; set; }

    [SugarColumn(Length = 256, IsNullable = true)]
    public string? AssemblyName { get; set; }

    // 为插件卸载等场景保留的兼容字段
    [SugarColumn(Length = 256, IsNullable = true)]
    public string? PluginAssembly { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ParamTemplate { get; set; }

    [SugarColumn(Length = 256, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(IsNullable = false)]
    public bool IsBuiltIn { get; set; } = false;

    // 兼容服务中的排序与状态筛选
    [SugarColumn(IsNullable = false)]
    public int OrderNo { get; set; } = 0;

    [SugarColumn(IsNullable = false)]
    public bool Status { get; set; } = true;
}