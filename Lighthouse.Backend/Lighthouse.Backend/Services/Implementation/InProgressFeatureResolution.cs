using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Turns the parent references of the work a Team has in progress into the Features they name: each
    /// reference once, in the order first met, read with one database statement. A reference nothing holds,
    /// or none at all, is answered by a placeholder saying so, so the Team's page still lists the work.
    /// A reference held by two Features is answered by the first the read gives, which is first in forecast order,
    /// and is reported to the caller so it can say so once.
    /// </summary>
    internal sealed class InProgressFeatureResolution(IFeatureRepository featureRepository)
    {
        private const string UnknownReference = "Unknown";

        public static IReadOnlyList<string?> ReferencesOf(IEnumerable<WorkItem> workInProgress)
            => workInProgress.Select(item => item.ParentReferenceId).Distinct().ToList();

        public IReadOnlyList<Feature> Resolve(IReadOnlyList<string?> references, Action<string> onStoredTwice)
        {
            var held = HeldByReference(references, onStoredTwice);

            return references
                .Select(reference => reference is not null && held.TryGetValue(reference, out var feature) ? feature : NotTracked(reference))
                .ToList();
        }

        private Dictionary<string, Feature> HeldByReference(IReadOnlyList<string?> references, Action<string> onStoredTwice)
        {
            var asked = references.OfType<string>().Where(reference => reference.Length > 0).Distinct().ToList();

            var byReference = featureRepository.FeaturesWithReferenceIds(asked)
                .GroupBy(feature => feature.ReferenceId, StringComparer.Ordinal)
                .ToList();

            foreach (var storedTwice in byReference.Where(group => group.Count() > 1))
            {
                onStoredTwice(storedTwice.Key);
            }

            return byReference.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
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
