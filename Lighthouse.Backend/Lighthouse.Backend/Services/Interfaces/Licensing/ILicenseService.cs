using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Licensing
{
    public interface ILicenseService
    {
        (LicenseInformation? licenseInfo, bool isValid) GetLicenseData();

        LicenseInformation? ImportLicense(string licenseContent);

        bool CanUsePremiumFeatures();
    }
}