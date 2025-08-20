using SqlSugar;

namespace DataProcess.DataSource.Core.Entity;

/// <summary>
/// 数据源实例
/// </summary>
[SugarTable("dp_datasource_instance", TableDescription = "数据源实例表")]
public class DataSourceInstance
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键")]
    public long Id { get; set; }

    [SugarColumn(ColumnDescription = "实例名称", Length = 64)]
    public string Name { get; set; } = default!;

    [SugarColumn(ColumnDescription = "类型编码", Length = 64)]
    public string TypeCode { get; set; } = default!;

    [SugarColumn(ColumnDescription = "连接配置(JSON)", Length = 2048)]
    public string ConfigJson { get; set; } = default!;

    [SugarColumn(ColumnDescription = "描述", Length = 256)]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "父实例Id")]
    public long? ParentId { get; set; }
}