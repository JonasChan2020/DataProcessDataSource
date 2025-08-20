using SqlSugar;

namespace DataProcess.DataSource.Core.Entity;

/// <summary>
/// ����Դʵ��
/// </summary>
[SugarTable("dp_datasource_instance", TableDescription = "����Դʵ����")]
public class DataSourceInstance
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "����")]
    public long Id { get; set; }

    [SugarColumn(ColumnDescription = "ʵ������", Length = 64)]
    public string Name { get; set; } = default!;

    [SugarColumn(ColumnDescription = "���ͱ���", Length = 64)]
    public string TypeCode { get; set; } = default!;

    [SugarColumn(ColumnDescription = "��������(JSON)", Length = 2048)]
    public string ConfigJson { get; set; } = default!;

    [SugarColumn(ColumnDescription = "����", Length = 256)]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "��ʵ��Id")]
    public long? ParentId { get; set; }
}