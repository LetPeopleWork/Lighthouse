using Lighthouse.Backend.Services.Interfaces;
using System.Reflection;

namespace Lighthouse.Backend.Services.Implementation
{
    public class AssemblyService : IAssemblyService
    {
        public string GetAssemblyVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
            return version;
        }
    }
}
