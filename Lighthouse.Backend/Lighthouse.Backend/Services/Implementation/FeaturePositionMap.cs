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
        private static readonly Comparer<FeatureOrderKey> OrderKeyComparer =
            Comparer<FeatureOrderKey>.Create((left, right) => FeatureComparer.CompareOrderValues(left.Order, right.Order));

        private readonly LighthouseAppContext context;

        public FeaturePositionMap(LighthouseAppContext context)
        {
            this.context = context;
        }

        public async Task<IReadOnlyDictionary<int, int>> GetAsync(CancellationToken cancellationToken = default)
        {
            // The SQL OrderBy feeds a stable in-memory sort, so equal order values tie-break by Id rather than by provider.
            var orderKeys = await context.Features
                .AsNoTracking()
                .OrderBy(feature => feature.Id)
                .Select(feature => new FeatureOrderKey(feature.Id, feature.Order))
                .ToListAsync(cancellationToken);

            var positions = new Dictionary<int, int>(orderKeys.Count);
            var position = 0;

            foreach (var orderKey in orderKeys.OrderBy(key => key, OrderKeyComparer))
            {
                positions[orderKey.Id] = ++position;
            }

            return positions;
        }
    }
}
