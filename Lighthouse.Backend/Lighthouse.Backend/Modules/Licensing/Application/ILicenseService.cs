using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.Licensing.Application
{
    public interface ILicenseService
    {
        (LicenseInformation? licenseInfo, bool isValid) GetLicenseData();

        Task<LicenseInformation?> ImportLicense(string licenseContent);

        bool CanUsePremiumFeatures();

        Task ClearLicense();
    }
}