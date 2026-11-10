using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IFeatureStateTransitionRepository : IRepository<FeatureStateTransition>
    {
        /// <summary>Every recorded state transition of these Features, tracked. One statement however many ids are asked for.</summary>
        IReadOnlyList<FeatureStateTransition> TransitionsOfFeatures(IReadOnlyCollection<int> featureIds);
    }
}
