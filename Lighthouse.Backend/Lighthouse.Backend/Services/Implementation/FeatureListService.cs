using System.Security.Claims;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureListService(
        IFeatureListReader reader,
        IRbacAdministrationService rbacAdministrationService,
        IFeaturePositionMap featurePositionMap,
        IFeatureMoveAuthorization featureMoveAuthorization,
        FeatureDependencyRendering dependencyRendering,
        FeatureListActiveWork activeWork,
        FeatureListOwners owners) : IFeatureListService
    {
        public bool PortfolioExists(int portfolioId) => owners.PortfolioExists(portfolioId);

        public bool TeamExists(int teamId) => owners.TeamExists(teamId);

        public async Task<FeatureListRead> ForPortfolioAsync(ClaimsPrincipal user, int portfolioId, CancellationToken cancellationToken)
        {
            return await ListOf(user, reader.FeaturesOfPortfolio(portfolioId), cancellationToken);
        }

        public async Task<FeatureListRead> ForTeamAsync(ClaimsPrincipal user, int teamId, CancellationToken cancellationToken)
        {
            return await ListOf(user, reader.FeaturesOfTeam(teamId), cancellationToken);
        }

        private async Task<FeatureListRead> ListOf(ClaimsPrincipal user, IReadOnlyList<Feature> held, CancellationToken cancellationToken)
        {
            var readablePortfolioIds = await ReadablePortfolioIds(user, held.SelectMany(feature => feature.Portfolios).Select(portfolio => portfolio.Id), cancellationToken);
            var readable = held.Where(feature => FeatureReadability.IsReadableBy(feature, readablePortfolioIds)).ToList();

            var positions = await featurePositionMap.GetAsync(cancellationToken);
            var verdicts = await featureMoveAuthorization.GetVerdictsAsync(user, readable, readablePortfolioIds, cancellationToken);
            var dependencies = await dependencyRendering.WhatTheseFeaturesWaitOn(user, readable, positions, everyFeatureIsLoaded: false, cancellationToken);
            var parents = await ParentsOf(user, readable, cancellationToken);
            var teamsAtWork = await activeWork.Of(user, readable, cancellationToken);

            return new FeatureListRead(readable, readablePortfolioIds, positions, verdicts, dependencies, parents, teamsAtWork);
        }

        private async Task<List<FeatureParent>> ParentsOf(ClaimsPrincipal user, IReadOnlyCollection<Feature> features, CancellationToken cancellationToken)
        {
            var references = features
                .Select(feature => feature.ParentReferenceId)
                .Where(reference => !string.IsNullOrEmpty(reference))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var parents = reader.ParentsOf(references);
            var readablePortfolioIds = await ReadablePortfolioIds(user, parents.SelectMany(parent => parent.PortfolioIds), cancellationToken);

            // A parent in no Portfolio is visible to everyone, as a Feature in none is.
            return [.. parents
                .Where(parent => parent.PortfolioIds.Count == 0 || parent.PortfolioIds.Any(readablePortfolioIds.Contains))
                .DistinctBy(parent => parent.ReferenceId, StringComparer.Ordinal)];
        }

        private async Task<HashSet<int>> ReadablePortfolioIds(ClaimsPrincipal user, IEnumerable<int> portfolioIds, CancellationToken cancellationToken)
        {
            var requested = portfolioIds.Distinct().ToArray();
            var readable = await rbacAdministrationService.GetReadablePortfolioIdsAsync(user, requested, cancellationToken).ConfigureAwait(false);

            return readable is { } ? readable.ToHashSet() : requested.ToHashSet();
        }
    }
}
