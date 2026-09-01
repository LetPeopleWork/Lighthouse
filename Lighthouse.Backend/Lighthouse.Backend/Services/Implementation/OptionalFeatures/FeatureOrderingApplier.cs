using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.OptionalFeatures
{
    /// <summary>
    /// Switching who decides the order Features are forecast in: hand out the places that are missing,
    /// store the new value, then have every Portfolio forecast again.
    /// </summary>
    public class FeatureOrderingApplier(
        IRepository<OptionalFeature> repository,
        IFeatureRankSeeder featureRankSeeder,
        IDomainEventDispatcher domainEventDispatcher) : IOptionalFeatureApplier
    {
        public string Key => OptionalFeatureKeys.FeatureOrderingKey;

        public async Task ApplyAsync(OptionalFeature feature, bool enabled)
        {
            if (enabled)
            {
                // Places are handed out on the way in only. Handing the order back must leave a Feature
                // that arrived in the meantime without one, because "no place" is how this instance
                // records that it was not choosing the order when that Feature showed up - and it is what
                // makes taking the order over again add to the end instead of renumbering everything.
                //
                // It also runs before the value changes, so the sequence it reads is still the one the
                // administrator was looking at when they flipped the switch.
                await featureRankSeeder.SeedMissingRanks();
            }

            feature.Enabled = enabled;
            repository.Update(feature);
            await repository.Save();

            // The seeding above is awaited here rather than announced, because losing it would leave an
            // instance owning the order with no places at all and nothing to say so. Re-forecasting is
            // announced, because a lost re-forecast costs one stale set of dates until the next refresh.
            var policy = enabled ? FeatureOrderingPolicy.ManualOrder : FeatureOrderingPolicy.SourceOrder;
            await domainEventDispatcher.PublishAsync(new FeatureOrderingPolicyChanged(policy));
        }
    }
}
