using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public record LicenseStatusDto
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

        // Left off the wire rather than sent as null: an empty field tells a viewer there is something
        // here they are not being shown.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; private init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Email { get; private init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Organization { get; private init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LicenseNumber { get; private init; }
        
        public DateTime? ExpiryDate { get; }

        public DateTime? ValidFrom { get; }

        public bool CanUsePremiumFeatures { get; }

        // Every signed-in caller gets this response, because the state of the license decides what the
        // interface offers them. Who the license belongs to is personal data and is not part of that.
        // The fields are named here so that the decision lives with the response, not at each call site.
        public LicenseStatusDto WithoutWhatOnlyAnAdministratorMaySee()
        {
            return this with { Name = null, Email = null, Organization = null, LicenseNumber = null };
        }
    }
}