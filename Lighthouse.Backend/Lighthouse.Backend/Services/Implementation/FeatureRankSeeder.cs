using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureRankSeeder(LighthouseAppContext context, IFeatureOrdering featureOrdering) : IFeatureRankSeeder
    {
        public async Task SeedMissingRanks()
        {
            // The same narrow projection the position map reads (ADR-135). Going through the Feature
            // repository instead would load the whole Include graph - Portfolios, work, Teams, forecasts -
            // and writing a place back over it re-inserts the join rows between a Portfolio and its Teams.
            var orderKeys = await context.Features
                .AsNoTracking()
                .Select(feature => new FeatureOrderKey(feature.Id, feature.Order, feature.ManualRank))
                .ToListAsync();

            var unplaced = featureOrdering.Order(orderKeys)
                .Where(orderKey => orderKey.ManualRank is null)
                .Select(orderKey => orderKey.Id)
                .ToList();

            if (unplaced.Count == 0)
            {
                return;
            }

            var lastPlace = orderKeys.Max(orderKey => orderKey.ManualRank) ?? 0;

            var features = await context.Features
                .Where(feature => unplaced.Contains(feature.Id))
                .ToDictionaryAsync(feature => feature.Id);

            foreach (var featureId in unplaced)
            {
                features[featureId].ManualRank = ++lastPlace;
            }

            await context.SaveChangesAsync();
        }
    }
}
