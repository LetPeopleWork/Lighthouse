using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class LicenseInformationRepository : RepositoryBase<LicenseInformation>
    {
        public LicenseInformationRepository(LighthouseAppContext context, ILogger<LicenseInformationRepository> logger) : base(context, context => context.LicenseInformation, logger)
        {
        }

        public override void Add(LicenseInformation item)
        {
            var existingLicenses = GetAll();

            if (existingLicenses.Any())
            {
                var existingLicense = existingLicenses.First();

                existingLicense.Name = item.Name;
                existingLicense.Email = item.Email;
                existingLicense.LicenseNumber = item.LicenseNumber;
                existingLicense.Organization = item.Organization;
                existingLicense.ExpiryDate = item.ExpiryDate;
                existingLicense.Signature = item.Signature;

                Update(existingLicense);
            }
            else
            {
                base.Add(item);
            }
        }
    }
}
