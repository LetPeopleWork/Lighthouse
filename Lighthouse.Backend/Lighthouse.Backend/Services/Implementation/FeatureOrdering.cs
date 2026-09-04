using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureOrdering(IFeatureOrderingPolicyProvider policyProvider) : IFeatureOrdering
    {
        private static readonly ManualRankComparer FeaturesByManualRank = new();

        private static readonly FeatureComparer FeaturesBySourceOrder = new();

        private static readonly Comparer<FeatureOrderKey> OrderKeysByManualRank =
            Comparer<FeatureOrderKey>.Create((left, right) => ManualRankComparer.CompareRanks(left.ManualRank, right.ManualRank));

        private static readonly Comparer<FeatureOrderKey> OrderKeysBySourceOrder =
            Comparer<FeatureOrderKey>.Create((left, right) => FeatureComparer.CompareOrderValues(left.Order, right.Order));

        public IEnumerable<Feature> Order(IEnumerable<Feature> features)
        {
            IComparer<Feature> comparer = ThisInstanceOwnsTheOrder() ? FeaturesByManualRank : FeaturesBySourceOrder;

            // Id is the second half of the key either way. Without it, rows tied on the leading value come
            // back in whatever sequence the store happened to hand them over in - which is not the same
            // sequence for a whole-table read and for one Portfolio's collection.
            return features.OrderBy(feature => feature, comparer).ThenBy(feature => feature.Id);
        }

        public IEnumerable<FeatureOrderKey> Order(IEnumerable<FeatureOrderKey> orderKeys)
        {
            return SortedWithTiesOnId(orderKeys, ThisInstanceOwnsTheOrder() ? OrderKeysByManualRank : OrderKeysBySourceOrder);
        }

        public IEnumerable<FeatureOrderKey> OrderBySourceOrder(IEnumerable<FeatureOrderKey> orderKeys)
        {
            return SortedWithTiesOnId(orderKeys, OrderKeysBySourceOrder);
        }

        private static IOrderedEnumerable<FeatureOrderKey> SortedWithTiesOnId(IEnumerable<FeatureOrderKey> orderKeys, Comparer<FeatureOrderKey> comparer)
        {
            return orderKeys.OrderBy(orderKey => orderKey, comparer).ThenBy(orderKey => orderKey.Id);
        }

        private bool ThisInstanceOwnsTheOrder() => policyProvider.GetPolicy() == FeatureOrderingPolicy.ManualOrder;
    }
}
