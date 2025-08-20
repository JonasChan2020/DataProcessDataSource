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

    /// <summary>关联的数据源类型编码（如：SqlServer、MySql）</summary>
    [SugarColumn(Length = 64, IsNullable = false, ColumnDescription = "类型编码")]
    public string TypeCode { get; set; } = default!;

    /// <summary>连接参数（JSON）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "连接参数（JSON）")]
    public string? Parameters { get; set; }

    [SugarColumn(IsNullable = false, ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}