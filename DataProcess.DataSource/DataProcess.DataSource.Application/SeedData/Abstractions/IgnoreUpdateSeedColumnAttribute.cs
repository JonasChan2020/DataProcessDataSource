using System;

namespace DataProcess.DataSource.Application.SeedData.Abstractions;

/// <summary>
/// 忽略更新的列（标记在实体属性上，更新时跳过这些列）
/// 目前未在本模块使用，预留对齐 Admin.NET 的能力
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreUpdateSeedColumnAttribute : Attribute
{
}