using System;

namespace DataProcess.DataSource.Application.SeedData.Abstractions;

/// <summary>
/// ���Ը��µ��У������ʵ�������ϣ�����ʱ������Щ�У�
/// Ŀǰδ�ڱ�ģ��ʹ�ã�Ԥ������ Admin.NET ������
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreUpdateSeedColumnAttribute : Attribute
{
}