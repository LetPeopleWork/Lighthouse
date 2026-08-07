using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureOrdering(IFeatureOrderingPolicyProvider policyProvider) : IFeatureOrdering
    {
        private static readonly Comparer<FeatureOrderKey> ByManualRank =
            Comparer<FeatureOrderKey>.Create((left, right) => ManualRankComparer.CompareRanks(left.ManualRank, right.ManualRank));

        private static readonly Comparer<FeatureOrderKey> BySourceOrder =
            Comparer<FeatureOrderKey>.Create((left, right) => FeatureComparer.CompareOrderValues(left.Order, right.Order));

        public IEnumerable<Feature> Order(IEnumerable<Feature> features)
        {
            var comparer = ThisInstanceOwnsTheOrder()
                ? new ManualRankComparer()
                : (IComparer<Feature>)new FeatureComparer();

            // Id is the second half of the key either way (INV-O1). Without it, rows tied on the leading
            // value come back in whatever sequence the store happened to hand them over in - which is not
            // the same sequence for a whole-table read and for one Portfolio's collection.
            return features.OrderBy(feature => feature, comparer).ThenBy(feature => feature.Id);
        }

        public IEnumerable<FeatureOrderKey> Order(IEnumerable<FeatureOrderKey> orderKeys)
        {
            var comparer = ThisInstanceOwnsTheOrder() ? ByManualRank : BySourceOrder;

            return orderKeys.OrderBy(orderKey => orderKey, comparer).ThenBy(orderKey => orderKey.Id);
        }

        private bool ThisInstanceOwnsTheOrder() => policyProvider.GetPolicy() == FeatureOrderingPolicy.ManualOrder;
    }
}
