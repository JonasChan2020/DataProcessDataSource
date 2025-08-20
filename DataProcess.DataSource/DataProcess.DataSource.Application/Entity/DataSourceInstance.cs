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

    /// <summary>����������Դ���ͱ��루�磺SqlServer��MySql��</summary>
    [SugarColumn(Length = 64, IsNullable = false, ColumnDescription = "���ͱ���")]
    public string TypeCode { get; set; } = default!;

    /// <summary>���Ӳ�����JSON��</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "���Ӳ�����JSON��")]
    public string? Parameters { get; set; }

    [SugarColumn(IsNullable = false, ColumnDescription = "�Ƿ�����")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "��ע")]
    public string? Remark { get; set; }
}