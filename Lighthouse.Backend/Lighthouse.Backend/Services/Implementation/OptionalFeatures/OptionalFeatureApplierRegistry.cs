using Lighthouse.Backend.Services.Interfaces.OptionalFeatures;

namespace Lighthouse.Backend.Services.Implementation.OptionalFeatures
{
    /// <summary>
    /// Which applier answers for which setting, read off the appliers that are registered rather than
    /// from a list of keys someone has to keep in step with them.
    /// </summary>
    public class OptionalFeatureApplierRegistry
    {
        private readonly Dictionary<string, IOptionalFeatureApplier> appliersByKey;

        private readonly DefaultOptionalFeatureApplier defaultApplier;

        public OptionalFeatureApplierRegistry(
            IEnumerable<IOptionalFeatureApplier> appliers,
            DefaultOptionalFeatureApplier defaultApplier)
        {
            appliersByKey = appliers.ToDictionary(applier => applier.Key, StringComparer.Ordinal);
            this.defaultApplier = defaultApplier;
        }

        /// <summary>
        /// The applier that claims this setting, or - deliberately, not by accident - the one that stores
        /// the value and does nothing else, which is what a setting nobody has claimed needs.
        /// </summary>
        public IOptionalFeatureApplier ApplierFor(string key)
        {
            return appliersByKey.TryGetValue(key, out var applier)
                ? applier
                : defaultApplier;
        }
    }
}
