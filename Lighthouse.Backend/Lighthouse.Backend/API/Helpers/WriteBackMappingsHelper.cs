using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Modules.Licensing.Application;

namespace Lighthouse.Backend.API.Helpers
{
    public static class WriteBackMappingsHelper
    {
        public static bool SupportsWriteBackMappings(this IEnumerable<WriteBackMappingDefinition> mappings,
            ILicenseService licenseService)
        {
            return !mappings.Any() || licenseService.CanUsePremiumFeatures();
        }
    }
}
