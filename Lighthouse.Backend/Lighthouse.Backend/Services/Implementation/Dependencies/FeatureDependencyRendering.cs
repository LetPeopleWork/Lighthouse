using System.Security.Claims;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    /// <summary>
    /// Works out, once for a whole page, which Features each of its rows is waiting on and what stands against
    /// each dependency. Every screen that shows a row reads the dependencies through here, because two screens
    /// disagreeing about one dependency is the failure the dependency decision exists to prevent.
    /// </summary>
    public class FeatureDependencyRendering(
        IFeatureListReader reader,
        IRbacAdministrationService rbacAdministrationService,
        IDependencyDecision dependencyDecision)
    {
        /// <summary>
        /// Which Features each of these is waiting on. A reference is only ever an id string the work tracking
        /// system wrote, so one naming something this instance does not hold cannot be named to a reader and is
        /// not here.
        /// </summary>
        /// <param name="everyFeatureIsLoaded">
        /// Whether the caller's list is already the whole graph, in which case it is used as it stands.
        /// </param>
        /// <remarks>
        /// The whole graph is read either way, because what is wrong with a dependency is a question about
        /// all of it - what is in a circle, what shares a Portfolio. Answering a request for a handful from
        /// that handful would report Features as unreachable merely because the request did not ask for
        /// them, and answering it with nothing would leave the same dependency warned about on one screen
        /// and silent on another. Two screens disagreeing about one dependency is the failure this feature
        /// exists to prevent, so the cost is paid rather than the answer withheld.
        /// </remarks>
        public async Task<DependenciesAsRead> WhatTheseFeaturesWaitOn(
            ClaimsPrincipal user,
            IReadOnlyCollection<Feature> features,
            IReadOnlyDictionary<int, int> positions,
            bool everyFeatureIsLoaded,
            CancellationToken cancellationToken)
        {
            var wholeGraph = everyFeatureIsLoaded
                ? features
                : reader.WholeGraphWithoutHistograms();

            var blockers = ByReferenceId(wholeGraph);

            var requestedPortfolioIds = blockers.Values
                .SelectMany(blocker => blocker.Portfolios)
                .Select(portfolio => portfolio.Id)
                .Distinct()
                .ToArray();

            var readable = await rbacAdministrationService
                .GetReadablePortfolioIdsAsync(user, requestedPortfolioIds, cancellationToken)
                .ConfigureAwait(false);

            var readablePortfolioIds = readable is { } ? readable.ToHashSet() : requestedPortfolioIds.ToHashSet();

            var verdicts = VerdictsBy(dependencyDecision.About(wholeGraph, positions));

            return new DependenciesAsRead(blockers, readablePortfolioIds, verdicts);
        }

        // Two Features can carry the same tracker id across Portfolios; a dependency names an id, so the
        // first of them stands for all of them - the same way the decision itself reads them.
        private static Dictionary<string, Feature> ByReferenceId(IReadOnlyCollection<Feature> features)
            => features
                .GroupBy(feature => feature.ReferenceId, StringComparer.Ordinal)
                .ToDictionary(byReferenceId => byReferenceId.Key, byReferenceId => byReferenceId.First(), StringComparer.Ordinal);

        private static Dictionary<(string Dependent, string Blocker), DependencyVerdict> VerdictsBy(HonouredDependencies decided)
            => decided.Verdicts
                .GroupBy(verdict => (verdict.DependentReferenceId, verdict.BlockerReferenceId))
                .ToDictionary(byPair => byPair.Key, byPair => byPair.First());
    }

    /// <summary>
    /// What one read worked out about dependencies, read once per row afterwards. A Feature the reader
    /// may not see is listed as withheld rather than dropped: a shorter list is one the reader has no
    /// way of telling is short.
    /// </summary>
    public sealed record DependenciesAsRead(
        Dictionary<string, Feature> Blockers,
        HashSet<int> ReadablePortfolioIds,
        Dictionary<(string Dependent, string Blocker), DependencyVerdict> Verdicts);
}
