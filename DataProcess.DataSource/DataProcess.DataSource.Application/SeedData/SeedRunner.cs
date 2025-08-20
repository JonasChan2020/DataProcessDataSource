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
/// ģ�鼶����ִ���������� Admin.NET ˼·��������������򼯣�
/// </summary>
internal static class SeedRunner
{
    public static void Execute(ISqlSugarClient db)
    {
        // ɨ��ʵ���˱�ģ�� ISqlSugarEntitySeedData<> ������
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

                var instance = Activator.CreateInstance(seedType) ?? throw new InvalidOperationException($"�޷�ʵ���� {seedType.FullName}");
                var hasDataMethod = iface.GetMethod(nameof(ISqlSugarEntitySeedData<object>.HasData))!;
                var dataObj = hasDataMethod.Invoke(instance, null) as IEnumerable;
                var dataList = dataObj?.Cast<object>().ToList() ?? new List<object>();
                if (dataList.Count == 0) continue;

                // ʹ�� ISqlSugarClient ֱ�ӽ��� Storageable������� Scope/Provider ������
                var storage = db.StorageableByObject(dataList).ToStorage();

                // ����
                storage.AsInsertable.ExecuteCommand();

                // �Ƿ���������
                var ignoreUpdate = seedType.GetCustomAttribute<IgnoreUpdateSeedAttribute>() != null;
                if (!ignoreUpdate)
                {
                    // ���Դ� IgnoreUpdateSeedColumnAttribute ����
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
                Console.WriteLine($"[SeedRunner] ��ʼ������ʧ�ܣ�{seedType.FullName}��ԭ��{ex.Message}");
                Console.ResetColor();
            }
        }
    }
}