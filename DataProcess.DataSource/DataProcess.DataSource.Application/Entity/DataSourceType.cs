using SqlSugar;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// ����Դ���ͱ�����/�����
/// </summary>
[SugarTable("dp_datasource_type", TableDescription = "����Դ���ͱ�")]
public class DataSourceType : EntityBase
{
    [SugarColumn(ColumnDescription = "��������", Length = 64)]
    public string Name { get; set; }

    [SugarColumn(ColumnDescription = "���ͱ���", Length = 64)]
    public string Code { get; set; }

    [SugarColumn(ColumnDescription = "����˵��", Length = 256, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "���������", Length = 128, IsNullable = true)]
    public string? PluginAssembly { get; set; }

    [SugarColumn(ColumnDescription = "����ģ��(JSON)", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ParamTemplateJson { get; set; }

    [SugarColumn(ColumnDescription = "�Ƿ���������")]
    public bool IsBuiltIn { get; set; }
}