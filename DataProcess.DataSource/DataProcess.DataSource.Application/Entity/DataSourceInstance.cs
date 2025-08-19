

using SqlSugar;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// ����Դʵ����
/// </summary>
[SugarTable("dp_datasource_instance", TableDescription = "����Դʵ����")]
public class DataSourceInstance : EntityBase
{
    [SugarColumn(ColumnDescription = "ʵ������", Length = 64)]
    public string Name { get; set; }

    [SugarColumn(ColumnDescription = "����Id")]
    public long TypeId { get; set; }

    [SugarColumn(ColumnDescription = "���ò���(JSON)", ColumnDataType = "nvarchar(max)")]
    public string ConfigJson { get; set; }

    [SugarColumn(ColumnDescription = "����ʵ��Id", IsNullable = true)]
    public long? ParentId { get; set; }
}