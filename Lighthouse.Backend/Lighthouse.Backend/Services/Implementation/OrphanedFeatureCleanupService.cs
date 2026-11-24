using System.Linq.Expressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    public class OrphanedFeatureCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrphanedFeatureCleanupService> logger)
        : IOrphanedFeatureCleanupService
    {
        private static readonly Expression<Func<Feature, bool>> IsInAPortfolio = f => f.Portfolios.Any();

        private static Expression<Func<Feature, bool>> ParentsAFeatureIn(IQueryable<Feature> featuresInAPortfolio) =>
            f => f.IsParentFeature && featuresInAPortfolio.Any(child => child.ParentReferenceId == f.ReferenceId);

        public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var featuresInAPortfolio = db.Features.Where(IsInAPortfolio);
            var parentsOfAPortfolioFeature = db.Features.Where(ParentsAFeatureIn(featuresInAPortfolio));

            var deleted = await db.Features
                .Where(f => !featuresInAPortfolio.Any(kept => kept.Id == f.Id)
                    && !parentsOfAPortfolioFeature.Any(kept => kept.Id == f.Id))
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted > 0)
            {
                logger.LogInformation("Cleaned up {Count} orphaned features", deleted);
            }

            // A Feature kept only as a parent name must not stay on any Team page or in any forecast, and both
            // select by the existence of a work row, so its rows are deleted rather than zeroed.
            await db.Set<FeatureWork>()
                .Where(work => !featuresInAPortfolio.Any(f => f.Id == work.FeatureId))
                .ExecuteDeleteAsync(cancellationToken);

            return deleted;
        }
    }
}
