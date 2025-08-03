using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class LicenseStatusDto
    {
        public LicenseStatusDto(LicenseInformation? licenseInfo, bool isValid)
        {
            HasLicense = licenseInfo != null;
            IsValid = isValid;
            
            if (HasLicense)
            {
                Name = licenseInfo.Name;
                Email = licenseInfo.Email;
                Organization = licenseInfo.Organization;
                ExpiryDate = licenseInfo.ExpiryDate;
            }
        }

        public bool HasLicense { get; }
        
        public bool IsValid { get; }

        public string? Name { get; }
        
        public string? Email { get; }
        
        public string? Organization { get; }
        
        public DateTime? ExpiryDate { get; }
    }
}