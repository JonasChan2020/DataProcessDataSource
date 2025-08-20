using System;

namespace DataProcess.DataSource.Application.SeedData.Abstractions;

/// <summary>
/// 忽略更新种子特性（标记在种子类，表示只插入不更新）
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreUpdateSeedAttribute : Attribute
{
}