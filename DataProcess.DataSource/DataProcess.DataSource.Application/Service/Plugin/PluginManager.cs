using System.Reflection;
using DataProcess.DataSource.Core.Plugin;
using Furion.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataProcess.DataSource.Application.Service.Plugin;

/// <summary>
/// 插件管理器
/// </summary>
public class PluginManager : ISingleton
{
    private readonly Dictionary<string, IDataSourceAdapter> _adapterCache = new();
    private readonly Dictionary<string, Assembly> _assemblyCache = new();
    private readonly string _pluginDirectory;

    public PluginManager()
    {
        _pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "datasource");
        Directory.CreateDirectory(_pluginDirectory);
    }

    /// <summary>
    /// 获取适配器实例
    /// </summary>
    public IDataSourceAdapter? GetAdapter(string? assemblyName, string? className)
    {
        if (string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(className))
            return null;

        var key = $"{assemblyName}#{className}";

        if (_adapterCache.TryGetValue(key, out var adapter))
            return adapter;

        try
        {
            var assembly = GetAssembly(assemblyName);
            if (assembly == null) return null;

            var type = assembly.GetType(className);
            if (type == null) return null;

            if (!typeof(IDataSourceAdapter).IsAssignableFrom(type))
                return null;

            adapter = (IDataSourceAdapter)Activator.CreateInstance(type)!;
            _adapterCache[key] = adapter;
            return adapter;
        }
        catch (Exception ex)
        {
            Log.Error($"创建适配器实例失败: {assemblyName}#{className}", ex);
            return null;
        }
    }

    /// <summary>
    /// 获取程序集
    /// </summary>
    private Assembly? GetAssembly(string assemblyName)
    {
        if (_assemblyCache.TryGetValue(assemblyName, out var assembly))
            return assembly;

        try
        {
            var dllPath = Path.Combine(_pluginDirectory, assemblyName, $"{assemblyName}.dll");
            if (!File.Exists(dllPath))
            {
                dllPath = Path.Combine(_pluginDirectory, $"{assemblyName}.dll");
                if (!File.Exists(dllPath))
                    return null;
            }

            assembly = Assembly.LoadFrom(dllPath);
            _assemblyCache[assemblyName] = assembly;
            return assembly;
        }
        catch (Exception ex)
        {
            Log.Error($"加载程序集失败: {assemblyName}", ex);
            return null;
        }
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    public void UnloadPlugin(string assemblyName)
    {
        var keysToRemove = _adapterCache.Keys.Where(k => k.StartsWith($"{assemblyName}#")).ToList();
        foreach (var key in keysToRemove)
        {
            _adapterCache.Remove(key);
        }

        _assemblyCache.Remove(assemblyName);
    }

    /// <summary>
    /// 安装插件
    /// </summary>
    public async Task<bool> InstallPluginAsync(Stream zipStream, string pluginName)
    {
        try
        {
            var pluginPath = Path.Combine(_pluginDirectory, pluginName);

            if (Directory.Exists(pluginPath))
                Directory.Delete(pluginPath, true);

            Directory.CreateDirectory(pluginPath);

            using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
            archive.ExtractToDirectory(pluginPath);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"安装插件失败: {pluginName}", ex);
            return false;
        }
    }

    /// <summary>
    /// 获取插件信息
    /// </summary>
    public async Task<PluginInfo?> GetPluginInfoAsync(string pluginName)
    {
        try
        {
            var configPath = Path.Combine(_pluginDirectory, pluginName, "plugin.json");
            if (!File.Exists(configPath))
                return null;

            var json = await File.ReadAllTextAsync(configPath);
            return JSON.Deserialize<PluginInfo>(json);
        }
        catch (Exception ex)
        {
            Log.Error($"读取插件信息失败: {pluginName}", ex);
            return null;
        }
    }
}

/// <summary>
/// 插件信息
/// </summary>
public class PluginInfo
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string AdapterClassName { get; set; } = string.Empty;
    public string ParamTemplate { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}