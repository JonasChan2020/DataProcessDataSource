// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core;

/// <summary>
/// 代码生成表
/// </summary>
[SugarTable(null, "代码生成表")]
[SysTable]
[SugarIndex("i_{table}_b", nameof(BusName), OrderByType.Asc)]
[SugarIndex("i_{table}_t", nameof(TableName), OrderByType.Asc)]
public partial class SysCodeGen : EntityBase
{
    /// <summary>
    /// 作者姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "作者姓名", Length = 32)]
    [MaxLength(32)]
    public string? AuthorName { get; set; }

    /// <summary>
    /// 是否移除表前缀
    /// </summary>
    [SugarColumn(ColumnDescription = "是否移除表前缀", Length = 8)]
    [MaxLength(8)]
    public string? TablePrefix { get; set; }

    /// <summary>
    /// 生成方式
    /// </summary>
    [SugarColumn(ColumnDescription = "生成方式", Length = 32)]
    [MaxLength(32)]
    public string? GenerateType { get; set; }

    /// <summary>
    /// 库定位器名
    /// </summary>
    [SugarColumn(ColumnDescription = "库定位器名", Length = 64)]
    [MaxLength(64)]
    public string? ConfigId { get; set; }

    /// <summary>
    /// 数据库名(保留字段)
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库库名", Length = 64)]
    [MaxLength(64)]
    public string? DbName { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库类型", DefaultValue = "4")]
    public SqlSugar.DbType DbType { get; set; } = SqlSugar.DbType.PostgreSQL;

    /// <summary>
    /// 数据库链接
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库链接", Length = 512)]
    [MaxLength(512)]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 数据库表名
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库表名", Length = 128)]
    [MaxLength(128)]
    public string? TableName { get; set; }

    /// <summary>
    /// 树控件名称
    /// </summary>
    [SugarColumn(ColumnDescription = "树控件名称", Length = 64)]
    [MaxLength(64)]
    public string? TreeName { get; set; }

    /// <summary>
    /// 命名空间
    /// </summary>
    [SugarColumn(ColumnDescription = "命名空间", Length = 128)]
    [MaxLength(128)]
    public string? NameSpace { get; set; }

    /// <summary>
    /// 业务名
    /// </summary>
    [SugarColumn(ColumnDescription = "业务名", Length = 128)]
    [MaxLength(128)]
    public string? BusName { get; set; }

    /// <summary>
    /// 表唯一字段配置
    /// </summary>
    [SugarColumn(ColumnDescription = "表唯一字段配置", Length = 128)]
    [MaxLength(128)]
    public string? TableUniqueConfig { get; set; }

    /// <summary>
    /// 是否生成菜单
    /// </summary>
    [SugarColumn(ColumnDescription = "是否生成菜单")]
    public bool GenerateMenu { get; set; } = true;

    /// <summary>
    /// 菜单图标
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单图标", Length = 32)]
    public string? MenuIcon { get; set; } = "ele-Menu";

    /// <summary>
    /// 菜单编码
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单编码")]
    public long? MenuPid { get; set; }

    /// <summary>
    /// 页面目录
    /// </summary>
    [SugarColumn(ColumnDescription = "页面目录", Length = 32)]
    public string? PagePath { get; set; }

    /// <summary>
    /// 支持打印类型
    /// </summary>
    [SugarColumn(ColumnDescription = "支持打印类型", Length = 32)]
    [MaxLength(32)]
    public string? PrintType { get; set; }

    /// <summary>
    /// 打印模版名称
    /// </summary>
    [SugarColumn(ColumnDescription = "打印模版名称", Length = 32)]
    [MaxLength(32)]
    public string? PrintName { get; set; }

    /// <summary>
    /// 左边树形结构表
    /// </summary>
    [SugarColumn(ColumnDescription = "左边树形结构表", Length = 64)]
    [MaxLength(64)]
    public string? LeftTab { get; set; }

    /// <summary>
    /// 左边关联字段
    /// </summary>
    [SugarColumn(ColumnDescription = "左边关联字段", Length = 64)]
    [MaxLength(64)]
    public string? LeftKey { get; set; }

    /// <summary>
    /// 左边关联主表字段
    /// </summary>
    [SugarColumn(ColumnDescription = "左边关联主表字段", Length = 64)]
    [MaxLength(64)]
    public string? LeftPrimaryKey { get; set; }

    /// <summary>
    /// 左边树名称
    /// </summary>
    [SugarColumn(ColumnDescription = "左边树名称", Length = 64)]
    [MaxLength(64)]
    public string? LeftName { get; set; }

    /// <summary>
    /// 下表名称
    /// </summary>
    [SugarColumn(ColumnDescription = "右区域下框表名称", Length = 64)]
    [MaxLength(64)]
    public string? BottomTab { get; set; }

    /// <summary>
    /// 下表关联字段
    /// </summary>
    [SugarColumn(ColumnDescription = "右区域下框表关联字段", Length = 64)]
    [MaxLength(64)]
    public string? BottomKey { get; set; }

    /// <summary>
    /// 下表关联主表字段
    /// </summary>
    [SugarColumn(ColumnDescription = "右区域下框表关联主表字段", Length = 64)]
    [MaxLength(64)]
    public string? BottomPrimaryKey { get; set; }

    /// <summary>
    /// 模板文件夹
    /// </summary>
    [SugarColumn(ColumnDescription = "模板文件夹", Length = 64)]
    [MaxLength(64)]
    public string? Template { get; set; }

    /// <summary>
    /// 表类型
    /// </summary>
    [SugarColumn(ColumnDescription = "表类型", Length = 64)]
    public string TabType { get; set; }

    /// <summary>
    /// 树控件PidKey字段
    /// </summary>
    [SugarColumn(ColumnDescription = "树控件PidKey字段", Length = 64)]
    [MaxLength(64)]
    public string? TreeKey { get; set; }

    /// <summary>
    /// 是否使用 Api Service
    /// </summary>
    [SugarColumn(ColumnDescription = "是否使用 Api Service")]
    public bool IsApiService { get; set; } = false;

    /// <summary>
    /// 模板关系：SysCodeGenTemplateRelation表中的CodeGenId，注意禁止给CodeGenTemplateRelations手动赋值
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SysCodeGenTemplateRelation.CodeGenId))]
    public List<SysCodeGenTemplateRelation> CodeGenTemplateRelations { get; set; }

    /// <summary>
    /// 表唯一字段列表
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual List<TableUniqueConfigItem> TableUniqueList => string.IsNullOrWhiteSpace(TableUniqueConfig) ? null : JSON.Deserialize<List<TableUniqueConfigItem>>(TableUniqueConfig);
}