using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Dependencies;

namespace Lighthouse.Backend.API.DTO
{
    public static class DependenciesAsReadExtensions
    {
        /// <summary>
        /// What one Feature waits on, as a row names it. A blocker the reader may not see is listed as withheld
        /// rather than dropped, because a shorter list is one the reader has no way of telling is short.
        /// </summary>
        public static List<FeatureDependsOnDto> Of(this DependenciesAsRead dependencies, Feature feature)
            => feature.DependsOnReferences
                .Where(reference => dependencies.Blockers.ContainsKey(reference.ReferenceId))
                .Select(reference => AnEntryFor(dependencies, feature, reference))
                .ToList();

        private static FeatureDependsOnDto AnEntryFor(DependenciesAsRead dependencies, Feature feature, FeatureDependencyReference reference)
        {
            var blocker = dependencies.Blockers[reference.ReferenceId];
            var verdict = dependencies.Verdicts.GetValueOrDefault((feature.ReferenceId, reference.ReferenceId));

            if (!FeatureReadability.IsReadableBy(blocker, dependencies.ReadablePortfolioIds))
            {
                return FeatureDependsOnDto.Withheld(reference.Source, verdict);
            }

            return new FeatureDependsOnDto(blocker, reference.Source, verdict);
        }
    }
}
