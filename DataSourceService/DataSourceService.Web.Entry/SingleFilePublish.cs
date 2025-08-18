using System.Reflection;
using Furion;

namespace DataSourceService.Web.Entry
{
    public class SingleFilePublish : ISingleFilePublish
    {
        public Assembly[] IncludeAssemblies()
        {
            return Array.Empty<Assembly>();
        }

        public string[] IncludeAssemblyNames()
        {
            return new[]
            {
                "DataSourceService.Application",
                "DataSourceService.Core",
                "DataSourceService.Web.Core"
            };
        }
    }
}