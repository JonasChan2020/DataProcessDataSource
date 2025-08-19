

using SqlSugar;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// 数据源实例表
/// </summary>
[SugarTable("dp_datasource_instance", TableDescription = "数据源实例表")]
public class DataSourceInstance : EntityBase
{
    [SugarColumn(ColumnDescription = "实例名称", Length = 64)]
    public string Name { get; set; }

    [SugarColumn(ColumnDescription = "类型Id")]
    public long TypeId { get; set; }

    [SugarColumn(ColumnDescription = "配置参数(JSON)", ColumnDataType = "nvarchar(max)")]
    public string ConfigJson { get; set; }

    [SugarColumn(ColumnDescription = "父级实例Id", IsNullable = true)]
    public long? ParentId { get; set; }
}