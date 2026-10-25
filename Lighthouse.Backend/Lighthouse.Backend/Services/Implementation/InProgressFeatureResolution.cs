using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Turns the parent references of the work a Team has in progress into the Features they name: each
    /// reference once, in the order first met, read with one database statement. A reference nothing holds,
    /// or none at all, is answered by a placeholder saying so, so the Team's page still lists the work.
    /// </summary>
    internal sealed class InProgressFeatureResolution(IFeatureRepository featureRepository)
    {
        private const string UnknownReference = "Unknown";

        public static IReadOnlyList<string?> ReferencesOf(IEnumerable<WorkItem> workInProgress)
            => workInProgress.Select(item => item.ParentReferenceId).Distinct().ToList();

        public IReadOnlyList<Feature> Resolve(IReadOnlyList<string?> references)
        {
            var held = HeldByReference(references);

            return references
                .Select(reference => reference is not null && held.TryGetValue(reference, out var feature) ? feature : NotTracked(reference))
                .ToList();
        }

        private Dictionary<string, Feature> HeldByReference(IReadOnlyList<string?> references)
        {
            var asked = references.OfType<string>().Where(reference => reference.Length > 0).Distinct().ToList();

            var held = new Dictionary<string, Feature>(StringComparer.Ordinal);
            foreach (var feature in featureRepository.FeaturesWithReferenceIds(asked))
            {
                held.TryAdd(feature.ReferenceId, feature);
            }

            return held;
        }

        private static Feature NotTracked(string? reference)
        {
            var shown = string.IsNullOrEmpty(reference) ? UnknownReference : reference;

            return new Feature
            {
                Id = -1,
                ReferenceId = shown,
                Name = $"{shown} (Item not tracked by Lighthouse)",
                Type = string.Empty,
                State = string.Empty,
                StateCategory = StateCategories.Unknown,
                Url = string.Empty,
                Order = string.Empty,
            };
        }
    }
}
