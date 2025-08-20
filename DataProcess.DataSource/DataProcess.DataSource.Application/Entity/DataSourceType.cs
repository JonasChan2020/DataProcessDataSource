using SqlSugar;
using DataProcess.DataSource.Core;
using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Application.Entity;

/// <summary>
/// ����Դ���ͱ�����/�����
/// </summary>
[SugarTable("dp_datasource_type", TableDescription = "����Դ���ͱ�")]
[SugarIndex("i_{table}_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("i_{table}_name", nameof(Name), OrderByType.Asc)]
public class DataSourceType : EntityBase
{
    /// <summary>
    /// ��������
    /// </summary>
    [SugarColumn(ColumnDescription = "��������", Length = 64)]
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ���ͱ��루Ψһ��
    /// </summary>
    [SugarColumn(ColumnDescription = "���ͱ���", Length = 64)]
    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// ����˵��
    /// </summary>
    [SugarColumn(ColumnDescription = "����˵��", Length = 256, IsNullable = true)]
    [MaxLength(256)]
    public string? Description { get; set; }

    /// <summary>
    /// ���������
    /// </summary>
    [SugarColumn(ColumnDescription = "���������", Length = 128, IsNullable = true)]
    [MaxLength(128)]
    public string? PluginAssembly { get; set; }

    /// <summary>
    /// ����������
    /// </summary>
    [SugarColumn(ColumnDescription = "����������", Length = 256, IsNullable = true)]
    [MaxLength(256)]
    public string? AdapterClassName { get; set; }

    /// <summary>
    /// ����ģ��(JSON)
    /// </summary>
    [SugarColumn(ColumnDescription = "����ģ��(JSON)", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ParamTemplateJson { get; set; }

    /// <summary>
    /// �Ƿ���������
    /// </summary>
    [SugarColumn(ColumnDescription = "�Ƿ���������")]
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// �����
    /// </summary>
    [SugarColumn(ColumnDescription = "�����")]
    public int OrderNo { get; set; } = 100;

    /// <summary>
    /// ״̬
    /// </summary>
    [SugarColumn(ColumnDescription = "״̬")]
    public bool Status { get; set; } = true;

    /// <summary>
    /// ͼ��
    /// </summary>
    [SugarColumn(ColumnDescription = "ͼ��", Length = 128, IsNullable = true)]
    [MaxLength(128)]
    public string? Icon { get; set; }

    /// <summary>
    /// �汾��
    /// </summary>
    [SugarColumn(ColumnDescription = "�汾��", Length = 32, IsNullable = true)]
    [MaxLength(32)]
    public string? Version { get; set; }
}