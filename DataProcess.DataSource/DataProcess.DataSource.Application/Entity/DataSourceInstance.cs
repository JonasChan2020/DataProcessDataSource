using SqlSugar;
using DataProcess.DataSource.Core;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// ����Դʵ���������������ã�
/// </summary>
[SugarTable("dp_ds_instance", TableDescription = "����Դʵ��")]
public class DataSourceInstance : EntityBase
{
    [SugarColumn(Length = 64, IsNullable = false, ColumnDescription = "ʵ�����룬Ψһ")]
    public string Code { get; set; } = default!;

    [SugarColumn(Length = 128, IsNullable = false, ColumnDescription = "ʵ������")]
    public string Name { get; set; } = default!;

    // ���ݣ�������ڰ�����Id��������ֹ�����ʽ
    [SugarColumn(IsNullable = true, ColumnDescription = "����Id")]
    public long? TypeId { get; set; }

    [SugarColumn(Length = 64, IsNullable = true, ColumnDescription = "���ͱ���")]
    public string? TypeCode { get; set; }

    // ����ʵ���̳�
    [SugarColumn(IsNullable = true, ColumnDescription = "��ʵ��Id")]
    public long? ParentId { get; set; }

    // ��ʵ���������ã�JSON��
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "�������ã�JSON��")]
    public string? OverrideJson { get; set; }

    // ���Ӳ�����JSON��
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "���Ӳ�����JSON��")]
    public string? Parameters { get; set; }

    // ������ʷ�ֶ�����ConfigJson
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "���Ӳ�����JSON�������ֶΣ�")]
    public string? ConfigJson { get; set; }

    [SugarColumn(IsNullable = false, ColumnDescription = "�Ƿ�����")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "��ע")]
    public string? Remark { get; set; }
}