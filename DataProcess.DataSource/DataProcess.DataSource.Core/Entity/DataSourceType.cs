using SqlSugar;

namespace DataProcess.DataSource.Core.Entity;

/// <summary>
/// 数据源类型（插件/内置）
/// </summary>
[SugarTable("dp_datasource_type", TableDescription = "数据源类型表")]
public class DataSourceType
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false, ColumnDescription = "主键")]
    public string Code { get; set; } = default!;

    [SugarColumn(ColumnDescription = "类型名称", Length = 64)]
    public string Name { get; set; } = default!;

    [SugarColumn(ColumnDescription = "描述", Length = 256)]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "版本", Length = 32)]
    public string? Version { get; set; }

    [SugarColumn(ColumnDescription = "适配器类名", Length = 256)]
    public string AdapterClassName { get; set; } = default!;

    [SugarColumn(ColumnDescription = "程序集名", Length = 128)]
    public string AssemblyName { get; set; } = default!;

    [SugarColumn(ColumnDescription = "参数模板", Length = 2048)]
    public string? ParamTemplate { get; set; }

    [SugarColumn(ColumnDescription = "图标", Length = 256)]
    public string? Icon { get; set; }

    [SugarColumn(ColumnDescription = "是否内置")]
    public bool IsBuiltIn { get; set; }
}