using SqlSugar;
using DataProcess.DataSource.Core;
using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// ����Դʵ����
/// </summary>
[SugarTable("dp_datasource_instance", TableDescription = "����Դʵ����")]
[SugarIndex("i_{table}_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("i_{table}_type", nameof(TypeId), OrderByType.Asc)]
[SugarIndex("i_{table}_parent", nameof(ParentId), OrderByType.Asc)]
public class DataSourceInstance : EntityBase
{
    /// <summary>
    /// ʵ������
    /// </summary>
    [SugarColumn(ColumnDescription = "ʵ������", Length = 64)]
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ʵ�����루Ψһ��
    /// </summary>
    [SugarColumn(ColumnDescription = "ʵ������", Length = 64)]
    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// ����Id
    /// </summary>
    [SugarColumn(ColumnDescription = "����Id")]
    public long TypeId { get; set; }

    /// <summary>
    /// ���ò���(JSON)
    /// </summary>
    [SugarColumn(ColumnDescription = "���ò���(JSON)", ColumnDataType = "nvarchar(max)")]
    [Required]
    public string ConfigJson { get; set; } = string.Empty;

    /// <summary>
    /// ����ʵ��Id
    /// </summary>
    [SugarColumn(ColumnDescription = "����ʵ��Id", IsNullable = true)]
    public long? ParentId { get; set; }

    /// <summary>
    /// ����״̬
    /// </summary>
    [SugarColumn(ColumnDescription = "����״̬")]
    public bool ConnectionStatus { get; set; } = false;

    /// <summary>
    /// �������ʱ��
    /// </summary>
    [SugarColumn(ColumnDescription = "�������ʱ��", IsNullable = true)]
    public DateTime? LastConnectTime { get; set; }

    /// <summary>
    /// ״̬
    /// </summary>
    [SugarColumn(ColumnDescription = "״̬")]
    public bool Status { get; set; } = true;

    /// <summary>
    /// ��ע
    /// </summary>
    [SugarColumn(ColumnDescription = "��ע", Length = 512, IsNullable = true)]
    [MaxLength(512)]
    public string? Remark { get; set; }

    /// <summary>
    /// �����
    /// </summary>
    [SugarColumn(ColumnDescription = "�����")]
    public int OrderNo { get; set; } = 100;
}