using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class LicenseStatusDto
    {
        public LicenseStatusDto(LicenseInformation? licenseInfo, bool isValid, bool canUsePremiumFeatures)
        {
            HasLicense = licenseInfo != null;
            IsValid = isValid;
            CanUsePremiumFeatures = canUsePremiumFeatures;

            if (HasLicense)
            {
                LicenseNumber = licenseInfo.LicenseNumber;
                Name = licenseInfo.Name;
                Email = licenseInfo.Email;
                Organization = licenseInfo.Organization;
                ExpiryDate = licenseInfo.ExpiryDate;
                ValidFrom = licenseInfo.ValidFrom;
            }
        }

        public bool HasLicense { get; }
        
        public bool IsValid { get; }

        public string? Name { get; }
        
        public string? Email { get; }
        
        public string? Organization { get; }

        public string? LicenseNumber { get; }
        
        public DateTime? ExpiryDate { get; }

        public DateTime? ValidFrom { get; }

        public bool CanUsePremiumFeatures { get; }
    }
}