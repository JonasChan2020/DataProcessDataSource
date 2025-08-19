// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统角色表格服务
/// </summary>
public class SysRoleTableService : ITransient
{
    private readonly SqlSugarRepository<SysRoleTable> _sysRoleTableRep;
    private readonly string _tableColumnDelimiter = ":"; // 表名和字段之间的分隔符

    public SysRoleTableService(SqlSugarRepository<SysRoleTable> sysRoleTableRep)
    {
        _sysRoleTableRep = sysRoleTableRep;
    }

    /// <summary>
    /// 获取角色表格字段集合
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    public async Task<List<string>> GetRoleTable(long roleId)
    {
        return await _sysRoleTableRep.AsQueryable()
            .Where(u => u.RoleId == roleId)
            .Select(u => $"{u.TableName}{_tableColumnDelimiter}{u.ColumnName}")
            .ToListAsync();
    }

    /// <summary>
    /// 授权角色表格
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task GrantRoleTable(RoleTableInput input)
    {
        await _sysRoleTableRep.AsDeleteable().Where(u => u.RoleId == input.Id).ExecuteCommandAsync();

        if (input.TableColumnList == null || input.TableColumnList.Count < 1)
            return;

        var sysRoleTableList = new List<SysRoleTable>();
        foreach (var item in input.TableColumnList)
        {
            // 过滤掉表名只保留字段名
            if (item.Contains(_tableColumnDelimiter))
            {
                var sysRoleTable = new SysRoleTable()
                {
                    RoleId = input.Id,
                    TableName = item.Split(_tableColumnDelimiter)[0],
                    ColumnName = item.Split(_tableColumnDelimiter)[1],
                };
                sysRoleTableList.Add(sysRoleTable);
            }
        }

        await _sysRoleTableRep.InsertRangeAsync(sysRoleTableList);
    }

    /// <summary>
    /// 获取当前用户表格字段集合
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="sysUserRoleService"></param>
    /// <returns></returns>
    public async Task<List<string>> GetUserRoleTableList(UserManager userManager, SysUserRoleService sysUserRoleService)
    {
        // 超管拥有所有表格字段
        if (userManager.SuperAdmin)
            return null;

        var roleIdList = await sysUserRoleService.GetUserRoleIdList(userManager.UserId);
        return await _sysRoleTableRep.AsQueryable()
            .Where(u => roleIdList.Contains(u.RoleId))
            .Select(u => $"{u.TableName}{_tableColumnDelimiter}{u.ColumnName}")
            .ToListAsync();
    }

    /// <summary>
    /// 整理所有表格字段
    /// </summary>
    /// <returns></returns>
    public List<RoleTableOutput> HandleTableColumn()
    {
        // 排除特定表格
        var ignoreTables = new List<string>
        {
            //"DingTalkUser",
            //"ApprovalFlow",
            //"GoViewPro",
            //"GoViewPro",
            //"GoViewProData"
        };
        // 排除特定字段
        var ignoreColumns = new List<string>
        {
            nameof(EntityBase.CreateTime),
            nameof(EntityBase.UpdateTime),
            nameof(EntityBase.CreateUserId),
            nameof(EntityBase.CreateUserName),
            nameof(EntityBase.UpdateUserId),
            nameof(EntityBase.UpdateUserName),
            nameof(EntityBase.IsDelete),
            nameof(EntityBase.Id),
        };

        var roleTableList = new List<RoleTableOutput>();
        // 遍历所有实体获取所有库表结构
        var entityTypes = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass && u.IsDefined(typeof(SugarTable), false)).ToList();
        foreach (var entityType in entityTypes)
        {
            var entityInfo = _sysRoleTableRep.Context.EntityMaintenance.GetEntityInfoNoCache(entityType);
            // 排除系统表和特定表
            if (!entityType.IsDefined(typeof(SysTableAttribute), false) && !ignoreTables.Contains(entityInfo.EntityName))
            {
                var table = new RoleTableOutput
                {
                    Id = PropertyNameToLower(entityInfo.EntityName),
                    Pid = "0",
                    Name = PropertyNameToLower(entityInfo.EntityName),
                    Label = entityInfo.TableDescription,
                };

                var columnList = new List<TableColumnOutput>();
                var columns = entityInfo.Columns.Where(u => !ignoreColumns.Contains(u.DbColumnName) && !u.IsIgnore).ToList();
                foreach (EntityColumnInfo columnInfo in columns)
                {
                    var column = new TableColumnOutput
                    {
                        Id = $"{PropertyNameToLower(entityInfo.EntityName)}{_tableColumnDelimiter}{PropertyNameToLower(columnInfo.DbColumnName)}",
                        Pid = PropertyNameToLower(entityInfo.EntityName),
                        Name = PropertyNameToLower(columnInfo.DbColumnName),
                        Label = columnInfo.ColumnDescription,
                    };
                    columnList.Add(column);
                }

                table.Columns = columnList;
                roleTableList.Add(table);
            }
        }

        return roleTableList;
    }

    /// <summary>
    /// 首字母小写
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    private static string PropertyNameToLower(string propertyName)
    {
        return string.IsNullOrWhiteSpace(propertyName) ? null : propertyName[..1].ToLower() + propertyName[1..];
    }

    /// <summary>
    /// 根据角色Id删除角色表格
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    public async Task DeleteRolTableByRoleId(long roleId)
    {
        await _sysRoleTableRep.DeleteAsync(u => u.RoleId == roleId);
    }

    /// <summary>
    /// 根据角色Id复制角色表格
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="newRoleId"></param>
    /// <returns></returns>
    public async Task CopyRolTableByRoleId(long roleId, long newRoleId)
    {
        var roleTableList = await _sysRoleTableRep.GetListAsync(u => u.RoleId == roleId);
        roleTableList.ForEach(u =>
        {
            u.Id = 0;
            u.RoleId = newRoleId;
        });
        await _sysRoleTableRep.InsertRangeAsync(roleTableList);
    }
}