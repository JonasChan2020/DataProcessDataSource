using System.ComponentModel.DataAnnotations;

namespace DataProcess.DataSource.Core;

// ����Id�������
public class BaseIdInput
{
    [Required(ErrorMessage = "Id����Ϊ��")]
    public virtual long Id { get; set; }
}

// ȫ�ַ�ҳ��ѯ�������
public class BasePageInput
{
    // ��ǰҳ�루��1��ʼ��
    public virtual int Page { get; set; } = 1;
    // ҳ����
    public virtual int PageSize { get; set; } = 20;
    // �����ֶΣ���ѡ��
    public virtual string? Field { get; set; }
    // ������asc/desc����ѡ��
    public virtual string? Order { get; set; }
}