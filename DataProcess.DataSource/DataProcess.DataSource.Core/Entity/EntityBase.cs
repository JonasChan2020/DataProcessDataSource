using SqlSugar;
using System;

namespace DataProcess.DataSource.Core;

/// <summary>
/// 框架实体基类Id
/// </summary>
public abstract class EntityBaseId
{
    /// <summary>
    /// 雪花Id
    /// </summary>
    [SugarColumn(ColumnName = "Id", ColumnDescription = "主键Id", IsPrimaryKey = true, IsIdentity = false)]
    public virtual long Id { get; set; }
}

/// <summary>
/// 框架实体基类
/// </summary>
[SugarIndex("i_{table}_ct", nameof(CreateTime), OrderByType.Asc)]
public abstract class EntityBase : EntityBaseId
{
    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnDescription = "创建时间", IsNullable = true, IsOnlyIgnoreUpdate = true)]
    public virtual DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnDescription = "更新时间", IsNullable = true)]
    public virtual DateTime? UpdateTime { get; set; }
}
