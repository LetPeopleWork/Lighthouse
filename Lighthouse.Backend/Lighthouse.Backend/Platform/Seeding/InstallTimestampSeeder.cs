using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Platform.Seeding;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;

namespace Lighthouse.Backend.Platform.Seeding
{
    public class InstallTimestampSeeder(
        IAppSettingService appSettingService,
        ILogger<InstallTimestampSeeder> logger) : ISeeder
    {
        public async Task Seed()
        {
            try
            {
                await appSettingService.EnsureInstallTimestamp();
            }
            catch (Exception probeFailure)
            {
                logger.LogError(probeFailure, "Install timestamp could not be established; the feedback nudge will stay not-eligible until the next successful startup");
            }
        }
    }
}
