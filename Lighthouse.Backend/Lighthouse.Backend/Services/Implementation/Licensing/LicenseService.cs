using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.Licensing
{
    public class LicenseService : ILicenseService
    {
        private readonly ILogger<LicenseService> logger;
        private readonly IRepository<LicenseInformation> licenseRepository;
        private readonly ILicenseVerifier licenseVerifier;

        public LicenseService(ILogger<LicenseService> logger, IRepository<LicenseInformation> licenseRepository, ILicenseVerifier licenseVerifier)
        {
            this.logger = logger;
            this.licenseRepository = licenseRepository;
            this.licenseVerifier = licenseVerifier;
        }

        public LicenseInformation? ImportLicense(string licenseContent)
        {
            try
            {
                var licenseInformation = ExtractLicenseInformation(licenseContent);

                var verifyLicense = licenseVerifier.VerifyLicense(licenseInformation);

                if (verifyLicense)
                {
                    licenseRepository.Add(licenseInformation);
                    licenseRepository.Save();
                }

                return licenseInformation;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while importing the license.");
                return null;
            }
        }

        public (LicenseInformation? licenseInfo, bool isValid) GetLicenseData()
        {
            var licenseInfo = licenseRepository.GetAll().FirstOrDefault();

            var isValid = licenseInfo != null && licenseVerifier.VerifyLicense(licenseInfo);

            return (licenseInfo, isValid);
        }

        public bool CanUsePremiumFeatures()
        {
            var (licenseInfo, isValid) = GetLicenseData();
            return isValid && licenseInfo?.ExpiryDate.Date >= DateTime.UtcNow.Date;
        }

        private LicenseInformation ExtractLicenseInformation(string license)
        {
            using JsonDocument licenseDoc = JsonDocument.Parse(license);

            var licenseElement = licenseDoc.RootElement.GetProperty("license");
            var signatureBase64 = licenseDoc.RootElement.GetProperty("signature").GetString();

            return new LicenseInformation
            {
                Name = licenseElement.GetProperty("name").GetString() ?? string.Empty,
                Email = licenseElement.GetProperty("email").GetString() ?? string.Empty,
                Organization = licenseElement.GetProperty("organization").GetString() ?? string.Empty,
                ExpiryDate = licenseElement.GetProperty("expiry").GetDateTime(),
                Signature = signatureBase64,
            };
        }
    }
}
