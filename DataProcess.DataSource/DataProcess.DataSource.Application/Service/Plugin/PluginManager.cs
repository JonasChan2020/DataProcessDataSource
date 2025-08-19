using System.Reflection;
using DataProcess.DataSource.Core.Plugin;

namespace DataProcess.DataSource.Application.Service.Plugin;

/// <summary>
/// 插件管理器（支持热插拔）
/// </summary>
public class PluginManager : ISingleton
{
    private readonly Dictionary<string, IDataSourcePlugin> _pluginCache = new();

    public IDataSourcePlugin? GetPlugin(string assemblyName)
    {
        if (_pluginCache.TryGetValue(assemblyName, out var plugin))
            return plugin;

        var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "datasource", assemblyName);
        var dllPath = Directory.GetFiles(pluginDir, "*.dll").FirstOrDefault();
        if (dllPath == null) return null;

        var asm = Assembly.LoadFrom(dllPath);
        var type = asm.GetTypes().FirstOrDefault(t => typeof(IDataSourcePlugin).IsAssignableFrom(t) && !t.IsInterface);
        if (type == null) return null;

        plugin = (IDataSourcePlugin)Activator.CreateInstance(type)!;
        _pluginCache[assemblyName] = plugin;
        return plugin;
    }

    public void UnloadPlugin(string assemblyName)
    {
        _pluginCache.Remove(assemblyName);
    }
}