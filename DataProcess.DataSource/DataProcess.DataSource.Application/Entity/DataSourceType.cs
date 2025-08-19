using SqlSugar;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// 数据源类型表（内置/插件）
/// </summary>
[SugarTable("dp_datasource_type", TableDescription = "数据源类型表")]
public class DataSourceType : EntityBase
{
    [SugarColumn(ColumnDescription = "类型名称", Length = 64)]
    public string Name { get; set; }

    [SugarColumn(ColumnDescription = "类型编码", Length = 64)]
    public string Code { get; set; }

    [SugarColumn(ColumnDescription = "类型说明", Length = 256, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "插件程序集名", Length = 128, IsNullable = true)]
    public string? PluginAssembly { get; set; }

    [SugarColumn(ColumnDescription = "参数模板(JSON)", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ParamTemplateJson { get; set; }

    [SugarColumn(ColumnDescription = "是否内置类型")]
    public bool IsBuiltIn { get; set; }
}