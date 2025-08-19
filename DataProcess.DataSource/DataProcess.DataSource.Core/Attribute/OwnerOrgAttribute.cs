

using System;

namespace DataProcess.DataSource.Core;

/// <summary>
/// 所属机构数据权限
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public class OwnerOrgAttribute : Attribute
{
}