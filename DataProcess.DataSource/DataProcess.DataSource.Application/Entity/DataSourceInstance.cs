using SqlSugar;
using DataProcess.DataSource.Core;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// 数据源实例（具体连接配置）
/// </summary>
[SugarTable("dp_ds_instance", TableDescription = "数据源实例")]
public class DataSourceInstance : EntityBase
{
    [SugarColumn(Length = 64, IsNullable = false, ColumnDescription = "实例编码，唯一")]
    public string Code { get; set; } = default!;

    [SugarColumn(Length = 128, IsNullable = false, ColumnDescription = "实例名称")]
    public string Name { get; set; } = default!;

    // 兼容：服务存在按类型Id或编码两种关联方式
    [SugarColumn(IsNullable = true, ColumnDescription = "类型Id")]
    public long? TypeId { get; set; }

    [SugarColumn(Length = 64, IsNullable = true, ColumnDescription = "类型编码")]
    public string? TypeCode { get; set; }

    // 父子实例继承
    [SugarColumn(IsNullable = true, ColumnDescription = "父实例Id")]
    public long? ParentId { get; set; }

    // 子实例覆盖配置（JSON）
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "覆盖配置（JSON）")]
    public string? OverrideJson { get; set; }

    // 连接参数（JSON）
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "连接参数（JSON）")]
    public string? Parameters { get; set; }

    // 兼容历史字段名：ConfigJson
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "连接参数（JSON，兼容字段）")]
    public string? ConfigJson { get; set; }

    [SugarColumn(IsNullable = false, ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}