using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Licensing.Application;

namespace Lighthouse.Backend.API.Helpers
{
    public static class AdditionalFieldsHelper
    {
        public static bool SupportsAdditionalFields(this IEnumerable<AdditionalFieldDefinition> additionalFields,
            ILicenseService licenseService)
        {
            return licenseService.CanUsePremiumFeatures() || additionalFields.Count(f => !f.IsPredefined) < 2;
        }
    }
}