using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.OptionalFeatures
{
    /// <summary>
    /// Storing the new value and nothing else, which is what switching every setting in the table does
    /// today. It answers for no setting in particular - it is what a setting gets when nobody has said it
    /// needs anything more - so it claims no key of its own.
    /// </summary>
    public class DefaultOptionalFeatureApplier(IRepository<OptionalFeature> repository) : IOptionalFeatureApplier
    {
        public string Key => string.Empty;

        public async Task ApplyAsync(OptionalFeature feature, bool enabled)
        {
            feature.Enabled = enabled;
            repository.Update(feature);
            await repository.Save();
        }
    }
}
