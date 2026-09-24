using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public sealed class OverTimeHistoryFillSwitch(IRepository<OptionalFeature> features) : IOverTimeHistoryFillSwitch
    {
        /// <summary>
        /// Selected by key because every stored optional feature carries the same Id, so a lookup by Id
        /// would match any of them. A row nobody has stored reads as off: an instance whose seeder has not
        /// run yet must not start writing days nobody opted in to.
        /// </summary>
        public bool IsSwitchedOn()
        {
            return features.GetByPredicate(feature => feature.Key == OptionalFeatureKeys.OverTimeHistoryFillKey)?.Enabled == true;
        }
    }
}
