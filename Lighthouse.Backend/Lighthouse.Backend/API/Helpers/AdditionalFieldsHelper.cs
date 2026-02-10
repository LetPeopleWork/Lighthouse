using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;

namespace Lighthouse.Backend.API.Helpers
{
    public static class AdditionalFieldsHelper
    {
        public static bool SupportsAdditionalFields(this IEnumerable<AdditionalFieldDefinition> additionalFields,
            ILicenseService licenseService)
        {
            return licenseService.CanUsePremiumFeatures() || additionalFields.Count() < 2;
        }
    }
}