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
            // The same narrow projection the position map reads. Going through the Feature
            // repository instead would load the whole Include graph - Portfolios, work, Teams, forecasts -
            // and writing a place back over it re-inserts the join rows between a Portfolio and its Teams.
            var orderKeys = await context.Features
                .AsNoTracking()
                .Select(feature => new FeatureOrderKey(feature.Id, feature.Order, feature.ManualRank))
                .ToListAsync();

            // Deliberately the sequence the tracker gave rather than the one this instance reads by. The
            // places being handed out here are the first places there have ever been, so asking who owns
            // the order would sort by a column that is still empty and renumber everything in row order.
            var unplaced = featureOrdering.OrderBySourceOrder(orderKeys)
                .Where(orderKey => orderKey.ManualRank is null)
                .Select(orderKey => orderKey.Id)
                .ToList();

            if (unplaced.Count == 0)
            {
                return;
            }

            // Re-read inside the write step and take only what is still unplaced. A rank written between
            // the projection above and here - by a move, or by a second admin flipping the switch at the
            // same moment - is somebody's chosen place and is never overwritten. Two seeds racing can
            // still land on the same number, which is harmless: duplicates keep a total order, because
            // the tie falls to Id.
            var features = await context.Features
                .Where(feature => unplaced.Contains(feature.Id) && feature.ManualRank == null)
                .ToDictionaryAsync(feature => feature.Id);

            if (features.Count == 0)
            {
                return;
            }

            var lastPlace = await context.Features.MaxAsync(feature => feature.ManualRank) ?? 0;

            foreach (var featureId in unplaced.Where(features.ContainsKey))
            {
                features[featureId].ManualRank = ++lastPlace;
            }

            await context.SaveChangesAsync();
        }
    }
}
