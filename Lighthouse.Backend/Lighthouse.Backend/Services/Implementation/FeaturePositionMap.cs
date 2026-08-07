using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Numbers the whole Features table from 1 by the same comparison every read path uses, before any RBAC
    /// filter runs - which is what makes the position global rather than an index into the caller's rows (ADR-135).
    /// </summary>
    public class FeaturePositionMap : IFeaturePositionMap
    {
        private readonly LighthouseAppContext context;
        private readonly IFeatureOrdering featureOrdering;

        public FeaturePositionMap(LighthouseAppContext context, IFeatureOrdering featureOrdering)
        {
            this.context = context;
            this.featureOrdering = featureOrdering;
        }

        public async Task<IReadOnlyDictionary<int, int>> GetAsync(CancellationToken cancellationToken = default)
        {
            var orderKeys = await context.Features
                .AsNoTracking()
                .Select(feature => new FeatureOrderKey(feature.Id, feature.Order, feature.ManualRank))
                .ToListAsync(cancellationToken);

            var positions = new Dictionary<int, int>(orderKeys.Count);
            var position = 0;

            foreach (var orderKey in featureOrdering.Order(orderKeys))
            {
                positions[orderKey.Id] = ++position;
            }

            return positions;
        }
    }
}
