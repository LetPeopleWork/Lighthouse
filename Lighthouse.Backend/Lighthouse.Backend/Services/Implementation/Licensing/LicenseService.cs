using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
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

        public async Task<LicenseInformation?> ImportLicense(string licenseContent)
        {
            try
            {
                var licenseInformation = ExtractLicenseInformation(licenseContent);

                var verifyLicense = licenseVerifier.VerifyLicense(licenseInformation);

                if (verifyLicense)
                {
                    licenseRepository.Add(licenseInformation);
                    await licenseRepository.Save();
                    return licenseInformation;
                }

                return null;
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
            if (!isValid || licenseInfo == null)
            {
                return false;
            }

            var now = DateTime.UtcNow.Date;
            var isNotExpired = licenseInfo.ExpiryDate.Date >= now;
            var isValidFromDate = !licenseInfo.ValidFrom.HasValue || licenseInfo.ValidFrom.Value.Date <= now;

            return isNotExpired && isValidFromDate;
        }

        public async Task ClearLicense()
        {
            var licenseInfo = licenseRepository.GetAll().FirstOrDefault();
            if (licenseInfo != null)
            {
                licenseRepository.Remove(licenseInfo.Id);
                await licenseRepository.Save();
            }
        }

        private LicenseInformation ExtractLicenseInformation(string license)
        {
            using JsonDocument licenseDoc = JsonDocument.Parse(license);

            var licenseElement = licenseDoc.RootElement.GetProperty("license");
            var signatureBase64 = licenseDoc.RootElement.GetProperty("signature").GetString();

            DateTime? validFrom = null;
            if (licenseElement.TryGetProperty("valid_from", out var validFromElement))
            {
                validFrom = DateTime.SpecifyKind(validFromElement.GetDateTime(), DateTimeKind.Utc);
            }

            return new LicenseInformation
            {
                Name = licenseElement.GetProperty("name").GetString() ?? string.Empty,
                Email = licenseElement.GetProperty("email").GetString() ?? string.Empty,
                Organization = licenseElement.GetProperty("organization").GetString() ?? string.Empty,
                ExpiryDate = DateTime.SpecifyKind(licenseElement.GetProperty("expiry").GetDateTime(), DateTimeKind.Utc),
                ValidFrom = validFrom,
                LicenseNumber = licenseElement.TryGetProperty("license_number", out var licenseNumberElement) 
                    ? licenseNumberElement.GetString() ?? string.Empty 
                    : string.Empty,
                Signature = signatureBase64,
            };
        }
    }
}
