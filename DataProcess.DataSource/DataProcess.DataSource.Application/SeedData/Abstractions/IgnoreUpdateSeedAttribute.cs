using System;

namespace DataProcess.DataSource.Application.SeedData.Abstractions;

/// <summary>
/// ���Ը����������ԣ�����������࣬��ʾֻ���벻���£�
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreUpdateSeedAttribute : Attribute
{
}