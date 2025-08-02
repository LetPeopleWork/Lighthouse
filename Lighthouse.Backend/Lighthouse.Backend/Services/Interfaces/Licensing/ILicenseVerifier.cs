using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Licensing
{
    public interface ILicenseVerifier
    {
        bool VerifyLicense(LicenseInformation license);
    }
}