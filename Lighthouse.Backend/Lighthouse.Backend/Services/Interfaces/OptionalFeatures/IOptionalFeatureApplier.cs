using Lighthouse.Backend.Models.OptionalFeatures;

namespace Lighthouse.Backend.Services.Interfaces.OptionalFeatures
{
    /// <summary>
    /// Everything that happens when one behaviour setting is switched, in one call. Deliberately not a
    /// pair of before/after hooks: a caller can invoke two hooks in the wrong order or forget the second,
    /// and a single method cannot be half-called.
    /// </summary>
    public interface IOptionalFeatureApplier
    {
        /// <summary>Which setting this applier answers for.</summary>
        string Key { get; }

        /// <summary>
        /// Carries out the whole write: any work that has to happen while the old value still stands, the
        /// change itself, storing it, and anything the rest of the system has to be told afterwards.
        /// </summary>
        Task ApplyAsync(OptionalFeature feature, bool enabled);
    }
}
