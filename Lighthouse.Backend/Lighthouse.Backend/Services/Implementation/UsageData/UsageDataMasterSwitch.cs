using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.UsageData;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    public sealed class UsageDataMasterSwitch(IRepository<OptionalFeature> features) : IUsageDataMasterSwitch
    {
        /// <summary>
        /// The key the optional feature is stored under. Held here rather than in each caller so
        /// that the switch and the things it governs cannot be renamed apart - a caller left on the
        /// old key would read no row, conclude "on", and go on sending after an administrator had
        /// switched it off.
        /// </summary>
        public const string Key = "UsageData";

        public bool IsOn()
        {
            return features.GetByPredicate(feature => feature.Key == Key)?.Enabled != false;
        }
    }
}
