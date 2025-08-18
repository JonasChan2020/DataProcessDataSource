using System.Collections.Generic;
using System.Linq;
using Furion;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using DataSourceService.Core.Attributes;

namespace DataSourceService.Core;

/// <summary>
/// 数据库上下文对象
/// </summary>
public static class DbContext
{
    /// <summary>
    /// SqlSugar 数据库实例
    /// </summary>
    public static readonly SqlSugarScope Instance = new(
        App.GetConfig<List<ConnectionConfig>>("DbConnection:ConnectionConfigs"),
        db =>
        {
            // 这里配置全局事件，比如拦截执行 SQL
        });

    static DbContext()
    {
        var connSection = App.Configuration.GetSection("DbConnection:ConnectionConfigs:0");
        var dbSettings = connSection.GetSection("DbSettings").Get<DbSettings>() ?? new();
        var tableSettings = connSection.GetSection("TableSettings").Get<TableSettings>() ?? new();

        if (dbSettings.EnableInitDb)
        {
            foreach (var conn in Instance.Ado.ConnectionConfigList)
            {
                var db = new SqlSugarClient(conn);
                db.DbMaintenance.CreateDatabase();
            }
        }

        var entityTypes = new[]
        {
            typeof(Entities.DataSourceType),
            typeof(Entities.DataSource)
        };

        if (tableSettings.EnableInitTable)
        {
            Instance.CodeFirst.InitTables(entityTypes);
        }
        else if (tableSettings.EnableIncreTable)
        {
            var increTypes = entityTypes
                .Where(t => t.IsDefined(typeof(IncreTableAttribute), true))
                .ToArray();
            if (increTypes.Length > 0)
            {
                Instance.CodeFirst.InitTables(increTypes);
            }
        }
    }

    private class DbSettings
    {
        public bool EnableInitDb { get; set; }
        public bool EnableInitView { get; set; }
        public bool EnableDiffLog { get; set; }
        public bool EnableUnderLine { get; set; }
        public bool EnableConnEncrypt { get; set; }
    }

    private class TableSettings
    {
        public bool EnableInitTable { get; set; }
        public bool EnableIncreTable { get; set; }
    }
}

