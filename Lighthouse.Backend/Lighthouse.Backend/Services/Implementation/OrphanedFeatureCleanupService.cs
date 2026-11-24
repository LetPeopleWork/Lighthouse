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
            var featuresInAPortfolio = db.Features.Where(f => f.Portfolios.Any());

            var deleted = await db.Features
                .Where(f => !f.Portfolios.Any()
                    && !(f.IsParentFeature && featuresInAPortfolio.Any(child => child.ParentReferenceId == f.ReferenceId)))
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted > 0)
            {
                logger.LogInformation("Cleaned up {Count} orphaned features", deleted);
            }
            return deleted;
        }
    }
}
