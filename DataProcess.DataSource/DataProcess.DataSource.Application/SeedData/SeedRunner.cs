using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Furion;
using SqlSugar;
using DataProcess.DataSource.Application.SeedData.Abstractions;

namespace DataProcess.DataSource.Application.SeedData;

/// <summary>
/// 模块级种子执行器（对齐 Admin.NET 思路，但不依赖其程序集）
/// </summary>
internal static class SeedRunner
{
    public static void Execute(ISqlSugarClient db)
    {
        // 扫描实现了本模块 ISqlSugarEntitySeedData<> 的种子
        var seedTypes = App.EffectiveTypes
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISqlSugarEntitySeedData<>)))
            .ToList();

        foreach (var seedType in seedTypes)
        {
            try
            {
                var iface = seedType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISqlSugarEntitySeedData<>));
                var entityType = iface.GetGenericArguments()[0];

                var instance = Activator.CreateInstance(seedType) ?? throw new InvalidOperationException($"无法实例化 {seedType.FullName}");
                var hasDataMethod = iface.GetMethod(nameof(ISqlSugarEntitySeedData<object>.HasData))!;
                var dataObj = hasDataMethod.Invoke(instance, null) as IEnumerable;
                var dataList = dataObj?.Cast<object>().ToList() ?? new List<object>();
                if (dataList.Count == 0) continue;

                // 使用 ISqlSugarClient 直接进行 Storageable，避免对 Scope/Provider 的依赖
                var storage = db.StorageableByObject(dataList).ToStorage();

                // 插入
                storage.AsInsertable.ExecuteCommand();

                // 是否跳过更新
                var ignoreUpdate = seedType.GetCustomAttribute<IgnoreUpdateSeedAttribute>() != null;
                if (!ignoreUpdate)
                {
                    // 忽略带 IgnoreUpdateSeedColumnAttribute 的列
                    var ignoreColumns = db.EntityMaintenance
                        .GetEntityInfo(entityType)
                        .Columns
                        .Where(c => c.PropertyInfo.GetCustomAttribute<IgnoreUpdateSeedColumnAttribute>() != null)
                        .Select(c => c.PropertyName)
                        .ToArray();

                    if (ignoreColumns.Length > 0)
                        storage.AsUpdateable.IgnoreColumns(ignoreColumns).ExecuteCommand();
                    else
                        storage.AsUpdateable.ExecuteCommand();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[SeedRunner] 初始化种子失败：{seedType.FullName}，原因：{ex.Message}");
                Console.ResetColor();
            }
        }
    }
}