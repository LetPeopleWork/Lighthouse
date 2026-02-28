using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces.Licensing;

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
