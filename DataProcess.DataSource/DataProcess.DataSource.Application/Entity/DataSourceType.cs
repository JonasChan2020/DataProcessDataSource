using SqlSugar;
using DataProcess.DataSource.Core;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// 数据源类型（业务实体，支持插件）
/// </summary>
[SugarTable("dp_ds_type", TableDescription = "数据源类型")]
public class DataSourceType : EntityBase
{
    [SugarColumn(Length = 64, IsNullable = false, ColumnDescription = "类型编码，唯一")]
    public string Code { get; set; } = default!;

    [SugarColumn(Length = 128, IsNullable = false, ColumnDescription = "类型名称")]
    public string Name { get; set; } = default!;

    [SugarColumn(Length = 512, IsNullable = true, ColumnDescription = "描述")]
    public string? Description { get; set; }

    [SugarColumn(Length = 32, IsNullable = true, ColumnDescription = "版本")]
    public string? Version { get; set; }

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "适配器实现类全名")]
    public string? AdapterClassName { get; set; }

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "适配器程序集名称")]
    public string? AssemblyName { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "参数模板（JSON）")]
    public string? ParamTemplate { get; set; }

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "图标")]
    public string? Icon { get; set; }

    [SugarColumn(IsNullable = false, ColumnDescription = "是否内置")]
    public bool IsBuiltIn { get; set; } = false;
}