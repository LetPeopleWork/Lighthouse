using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// ADR-135: one narrow projection over the whole Features table — no <c>Include</c> graph — ordered by
    /// the same comparison every read path uses, then numbered from 1. Numbering happens before the RBAC
    /// filter, which is what makes the position global rather than an index into what the caller may see.
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
