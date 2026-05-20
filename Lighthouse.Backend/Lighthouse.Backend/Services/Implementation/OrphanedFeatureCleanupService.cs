using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    public class OrphanedFeatureCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrphanedFeatureCleanupService> logger)
        : IOrphanedFeatureCleanupService
    {
        public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var deleted = await db.Features
                .Where(f => !f.IsParentFeature && !f.Portfolios.Any())
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted > 0)
            {
                logger.LogInformation("Cleaned up {Count} orphaned features", deleted);
            }
            return deleted;
        }
    }
}
