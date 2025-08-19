using System.Reflection;
using Furion;

namespace DataProcess.DataSource.Web.Entry
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
                "DataProcess.DataSource.Application",
                "DataProcess.DataSource.Core",
                "DataProcess.DataSource.Web.Core"
            };
        }
    }
}