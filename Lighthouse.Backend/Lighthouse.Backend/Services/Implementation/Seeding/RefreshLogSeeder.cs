using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Seeding;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    public class RefreshLogSeeder(
        IRefreshLogService refreshLogService,
        ILogger<RefreshLogSeeder> logger) : ISeeder
    {
        public async Task Seed()
        {
            logger.LogInformation("Cleaning up orphaned Refresh Logs");
            await refreshLogService.RemoveOrphanedRefreshLogs();
            logger.LogInformation("Orphaned Refresh Logs cleaned up successfully");
        }
    }
}
