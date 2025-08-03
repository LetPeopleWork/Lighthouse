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
                Update(item);
            }
            else
            {
                base.Add(item);
            }
        }
    }
}
