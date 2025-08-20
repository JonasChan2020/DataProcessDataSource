using System.Reflection;
using DataProcess.DataSource.Core.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataProcess.DataSource.Application.Service.Plugin;

/// <summary>
/// ���������
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
    /// ��ȡ������ʵ��
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
            Log.Error($"����������ʵ��ʧ��: {assemblyName}#{className}", ex);
            return null;
        }
    }

    /// <summary>
    /// ��ȡ����
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
            Log.Error($"���س���ʧ��: {assemblyName}", ex);
            return null;
        }
    }

    /// <summary>
    /// ж�ز��
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
    /// ��װ���
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
            Log.Error($"��װ���ʧ��: {pluginName}", ex);
            return false;
        }
    }

    /// <summary>
    /// ��ȡ�����Ϣ
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
            Log.Error($"��ȡ�����Ϣʧ��: {pluginName}", ex);
            return null;
        }
    }
}

/// <summary>
/// �����Ϣ
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