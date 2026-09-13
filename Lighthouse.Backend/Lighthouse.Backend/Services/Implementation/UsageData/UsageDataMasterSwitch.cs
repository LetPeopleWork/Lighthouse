using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.UsageData;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    public sealed class UsageDataMasterSwitch(IRepository<OptionalFeature> features) : IUsageDataMasterSwitch
    {
        /// <summary>
        /// The key the veto is stored under, and the same constant the seeder writes. One definition
        /// rather than two, because a caller left on an old key would read no row, conclude that
        /// nothing was vetoed, and go on sending after an administrator had stopped it.
        /// </summary>
        public const string Key = OptionalFeatureKeys.UsageDataKey;

        /// <summary>
        /// Whether usage data may flow at all on this instance. It is not the same question as
        /// whether any particular person agreed - that is asked separately, and this one comes
        /// first.
        ///
        /// The row is a veto rather than a permission: stored true means an administrator has
        /// stopped usage data for everybody, so this answers false. A row nobody has written means
        /// nobody has stopped anything, which is why an absent row reads as allowed - an instance
        /// between a release landing and its seeder running must behave as it did the day before,
        /// not go silent for reasons nothing reports.
        /// </summary>
        public bool IsAllowed()
        {
            return features.GetByPredicate(feature => feature.Key == Key)?.Enabled != true;
        }
    }
}
