using System.Reflection;
using System.Runtime.Loader;
using DataProcess.DataSource.Core.Plugin;

namespace DataProcess.DataSource.Application.Service.Plugin;

/// <summary>
/// ≤Âº˛…≥œ‰∏Ù¿Îº”‘ÿ∆˜
/// </summary>
public class PluginSandboxManager : ISingleton
{
    private readonly Dictionary<string, (AssemblyLoadContext, IDataSourcePlugin)> _pluginContexts = new();

    public IDataSourcePlugin? LoadPlugin(string pluginPath, string assemblyName)
    {
        if (_pluginContexts.ContainsKey(assemblyName))
            return _pluginContexts[assemblyName].Item2;

        var alc = new AssemblyLoadContext(assemblyName, isCollectible: true);
        var asm = alc.LoadFromAssemblyPath(pluginPath);
        var type = asm.GetTypes().FirstOrDefault(t => typeof(IDataSourcePlugin).IsAssignableFrom(t) && !t.IsInterface);
        if (type == null) return null;

        var plugin = (IDataSourcePlugin)Activator.CreateInstance(type)!;
        _pluginContexts[assemblyName] = (alc, plugin);
        return plugin;
    }

    public void UnloadPlugin(string assemblyName)
    {
        if (_pluginContexts.TryGetValue(assemblyName, out var tuple))
        {
            tuple.Item1.Unload();
            _pluginContexts.Remove(assemblyName);
        }
    }
}