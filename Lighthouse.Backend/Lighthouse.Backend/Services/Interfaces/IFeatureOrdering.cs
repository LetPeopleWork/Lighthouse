using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// The single selection point (ADR-134 SA-2). The only production type that constructs an ordering
    /// comparer, so every read path - the Features view, the Portfolio detail, the forecast queue and
    /// the position map - draws from one sequence rather than four that happen to agree.
    /// </summary>
    public interface IFeatureOrdering
    {
        IEnumerable<Feature> Order(IEnumerable<Feature> features);

        IEnumerable<FeatureOrderKey> Order(IEnumerable<FeatureOrderKey> orderKeys);
    }
}
