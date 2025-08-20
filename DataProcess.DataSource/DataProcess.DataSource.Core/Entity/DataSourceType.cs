using SqlSugar;

namespace DataProcess.DataSource.Core.Entity;

/// <summary>
/// ����Դ���ͣ����/���ã�
/// </summary>
[SugarTable("dp_datasource_type", TableDescription = "����Դ���ͱ�")]
public class DataSourceType
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false, ColumnDescription = "����")]
    public string Code { get; set; } = default!;

    [SugarColumn(ColumnDescription = "��������", Length = 64)]
    public string Name { get; set; } = default!;

    [SugarColumn(ColumnDescription = "����", Length = 256)]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "�汾", Length = 32)]
    public string? Version { get; set; }

    [SugarColumn(ColumnDescription = "����������", Length = 256)]
    public string AdapterClassName { get; set; } = default!;

    [SugarColumn(ColumnDescription = "������", Length = 128)]
    public string AssemblyName { get; set; } = default!;

    [SugarColumn(ColumnDescription = "����ģ��", Length = 2048)]
    public string? ParamTemplate { get; set; }

    [SugarColumn(ColumnDescription = "ͼ��", Length = 256)]
    public string? Icon { get; set; }

    [SugarColumn(ColumnDescription = "�Ƿ�����")]
    public bool IsBuiltIn { get; set; }
}