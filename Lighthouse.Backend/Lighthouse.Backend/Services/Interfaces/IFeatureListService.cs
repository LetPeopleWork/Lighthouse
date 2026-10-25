using System.Security.Claims;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>The Features list of an owner, read once and complete: its rows, their parents and who works on them today.</summary>
    public interface IFeatureListService
    {
        /// <summary>Whether the Portfolio exists and, if it does, whether the caller may open its list.</summary>
        Task<FeatureListAccess> AccessToPortfolioAsync(ClaimsPrincipal user, int portfolioId, CancellationToken cancellationToken);

        /// <summary>Whether the Team exists and, if it does, whether the caller may open its list.</summary>
        Task<FeatureListAccess> AccessToTeamAsync(ClaimsPrincipal user, int teamId, CancellationToken cancellationToken);

        /// <summary>
        /// The Features of a Portfolio in forecast order, without those the caller may not read, with what each waits
        /// on, which Teams the caller may read are at work on them, and the parents the caller may read.
        /// </summary>
        Task<FeatureListRead> ForPortfolioAsync(ClaimsPrincipal user, int portfolioId, CancellationToken cancellationToken);

        /// <summary>The Features a Team works on across Portfolios, read and filtered as a Portfolio's are.</summary>
        Task<FeatureListRead> ForTeamAsync(ClaimsPrincipal user, int teamId, CancellationToken cancellationToken);
    }

    /// <summary>Whether a caller may open the list of an owner.</summary>
    public enum FeatureListAccess
    {
        Granted,
        Refused,
        OwnerMissing,
    }

    /// <summary>What one read of a list worked out. Turning it into the rows of an answer is the caller's.</summary>
    public sealed record FeatureListRead(
        IReadOnlyList<Feature> Features,
        HashSet<int> ReadablePortfolioIds,
        IReadOnlyDictionary<int, int> Positions,
        IReadOnlyDictionary<int, FeatureMoveVerdict> Verdicts,
        DependenciesAsRead Dependencies,
        IReadOnlyList<FeatureParent> Parents,
        IReadOnlyList<TeamAtWork> ActiveWork);

    /// <summary>The Features, by reference id, a Team has work in progress under today.</summary>
    public sealed record TeamAtWork(int TeamId, IReadOnlyList<string> FeatureReferenceIds);
}
