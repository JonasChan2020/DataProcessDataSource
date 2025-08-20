using SqlSugar;
using DataProcess.DataSource.Core;
using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// 数据源类型表（内置/插件）
/// </summary>
[SugarTable("dp_datasource_type", TableDescription = "数据源类型表")]
[SugarIndex("i_{table}_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("i_{table}_name", nameof(Name), OrderByType.Asc)]
public class DataSourceType : EntityBase
{
    /// <summary>
    /// 类型名称
    /// </summary>
    [SugarColumn(ColumnDescription = "类型名称", Length = 64)]
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型编码（唯一）
    /// </summary>
    [SugarColumn(ColumnDescription = "类型编码", Length = 64)]
    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 类型说明
    /// </summary>
    [SugarColumn(ColumnDescription = "类型说明", Length = 256, IsNullable = true)]
    [MaxLength(256)]
    public string? Description { get; set; }

    /// <summary>
    /// 插件程序集名
    /// </summary>
    [SugarColumn(ColumnDescription = "插件程序集名", Length = 128, IsNullable = true)]
    [MaxLength(128)]
    public string? PluginAssembly { get; set; }

    /// <summary>
    /// 适配器类名
    /// </summary>
    [SugarColumn(ColumnDescription = "适配器类名", Length = 256, IsNullable = true)]
    [MaxLength(256)]
    public string? AdapterClassName { get; set; }

    /// <summary>
    /// 参数模板(JSON)
    /// </summary>
    [SugarColumn(ColumnDescription = "参数模板(JSON)", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ParamTemplateJson { get; set; }

    /// <summary>
    /// 是否内置类型
    /// </summary>
    [SugarColumn(ColumnDescription = "是否内置类型")]
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnDescription = "排序号")]
    public int OrderNo { get; set; } = 100;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public bool Status { get; set; } = true;

    /// <summary>
    /// 图标
    /// </summary>
    [SugarColumn(ColumnDescription = "图标", Length = 128, IsNullable = true)]
    [MaxLength(128)]
    public string? Icon { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [SugarColumn(ColumnDescription = "版本号", Length = 32, IsNullable = true)]
    [MaxLength(32)]
    public string? Version { get; set; }
}