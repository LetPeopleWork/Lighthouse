using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// The only production type that builds an ordering comparer, so every read path - the Features view,
    /// the Portfolio detail, the forecast queue and the position map - draws from one sequence rather
    /// than four that happen to agree.
    /// </summary>
    public interface IFeatureOrdering
    {
        IEnumerable<Feature> Order(IEnumerable<Feature> features);

        IEnumerable<FeatureOrderKey> Order(IEnumerable<FeatureOrderKey> orderKeys);

        /// <summary>
        /// The sequence the work tracking system gave, whoever owns the order on this instance. Handing
        /// out first places is the one job that must not consult the setting: read it and the answer
        /// depends on whether the places were handed out before or after the switch was flipped, and
        /// after the flip there are no places yet to sort by, so every Feature gets renumbered in
        /// whatever sequence the store happened to hand the rows over.
        /// </summary>
        IEnumerable<FeatureOrderKey> OrderBySourceOrder(IEnumerable<FeatureOrderKey> orderKeys);
    }
}
