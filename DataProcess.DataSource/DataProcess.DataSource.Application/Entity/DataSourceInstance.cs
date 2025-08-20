using SqlSugar;
using DataProcess.DataSource.Core;
using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// 数据源实例表
/// </summary>
[SugarTable("dp_datasource_instance", TableDescription = "数据源实例表")]
[SugarIndex("i_{table}_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("i_{table}_type", nameof(TypeId), OrderByType.Asc)]
[SugarIndex("i_{table}_parent", nameof(ParentId), OrderByType.Asc)]
public class DataSourceInstance : EntityBase
{
    /// <summary>
    /// 实例名称
    /// </summary>
    [SugarColumn(ColumnDescription = "实例名称", Length = 64)]
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实例编码（唯一）
    /// </summary>
    [SugarColumn(ColumnDescription = "实例编码", Length = 64)]
    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 类型Id
    /// </summary>
    [SugarColumn(ColumnDescription = "类型Id")]
    public long TypeId { get; set; }

    /// <summary>
    /// 配置参数(JSON)
    /// </summary>
    [SugarColumn(ColumnDescription = "配置参数(JSON)", ColumnDataType = "nvarchar(max)")]
    [Required]
    public string ConfigJson { get; set; } = string.Empty;

    /// <summary>
    /// 父级实例Id
    /// </summary>
    [SugarColumn(ColumnDescription = "父级实例Id", IsNullable = true)]
    public long? ParentId { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    [SugarColumn(ColumnDescription = "连接状态")]
    public bool ConnectionStatus { get; set; } = false;

    /// <summary>
    /// 最后连接时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后连接时间", IsNullable = true)]
    public DateTime? LastConnectTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public bool Status { get; set; } = true;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 512, IsNullable = true)]
    [MaxLength(512)]
    public string? Remark { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnDescription = "排序号")]
    public int OrderNo { get; set; } = 100;
}