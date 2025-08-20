using SqlSugar;
using DataProcess.DataSource.Core;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// ����Դ���ͣ�ҵ��ʵ�壬֧�ֲ����
/// </summary>
[SugarTable("dp_ds_type", TableDescription = "����Դ����")]
public class DataSourceType : EntityBase
{
    [SugarColumn(Length = 64, IsNullable = false, ColumnDescription = "���ͱ��룬Ψһ")]
    public string Code { get; set; } = default!;

    [SugarColumn(Length = 128, IsNullable = false, ColumnDescription = "��������")]
    public string Name { get; set; } = default!;

    [SugarColumn(Length = 512, IsNullable = true, ColumnDescription = "����")]
    public string? Description { get; set; }

    [SugarColumn(Length = 32, IsNullable = true, ColumnDescription = "�汾")]
    public string? Version { get; set; }

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "������ʵ����ȫ��")]
    public string? AdapterClassName { get; set; }

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "��������������")]
    public string? AssemblyName { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "����ģ�壨JSON��")]
    public string? ParamTemplate { get; set; }

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "ͼ��")]
    public string? Icon { get; set; }

    [SugarColumn(IsNullable = false, ColumnDescription = "�Ƿ�����")]
    public bool IsBuiltIn { get; set; } = false;
}