using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.Licensing.Application
{
    public interface ILicenseVerifier
    {
        bool VerifyLicense(LicenseInformation license);
    }
}