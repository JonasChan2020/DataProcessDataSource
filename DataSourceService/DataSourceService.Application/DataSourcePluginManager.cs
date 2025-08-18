namespace DataSourceService.Application;

using System.Reflection;
using Common.Interfaces;

public class DataSourcePluginManager : ITransient
{
    private readonly Dictionary<string, IDataSourceAdapter> _adapters = new();

    public void LoadAll()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "plugins", "datasource");
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "*.dll"))
        {
            LoadAsync(file).GetAwaiter().GetResult();
        }
    }

    public async Task LoadAsync(string pluginPath)
    {
        var asm = Assembly.LoadFrom(pluginPath);
        foreach (var type in asm.GetTypes())
        {
            if (typeof(IDataSourcePlugin).IsAssignableFrom(type) && !type.IsAbstract)
            {
                if (Activator.CreateInstance(type) is IDataSourcePlugin plugin)
                {
                    var adapter = plugin.CreateAdapter();
                    _adapters[plugin.TypeCode] = adapter;
                }
            }
        }
        await Task.CompletedTask;
    }

    public IDataSourceAdapter? GetAdapter(string typeCode)
    {
        _adapters.TryGetValue(typeCode, out var adapter);
        return adapter;
    }

    public void Unload(string typeCode)
    {
        _adapters.Remove(typeCode);
    }
}
