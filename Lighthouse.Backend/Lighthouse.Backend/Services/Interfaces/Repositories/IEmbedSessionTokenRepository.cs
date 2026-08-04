using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    // Deliberately not IRepository<T>: single use is a conditional update returning an affected-row
    // count, which the generic add/save shape cannot express (ADR-131).
    public interface IEmbedSessionTokenRepository
    {
        Task AddAsync(EmbedSessionToken token, CancellationToken cancellationToken);

        Task<EmbedSessionToken?> FindByTokenIdAsync(string tokenId, CancellationToken cancellationToken);

        /// <summary>
        /// Marks the token redeemed only if it is still redeemable, and reports how many rows that
        /// affected. Racing callers see exactly one 1 and the rest 0.
        /// </summary>
        Task<int> TryMarkRedeemedAsync(string tokenId, DateTime redeemedAt, CancellationToken cancellationToken);

        Task<int> RevokeOutstandingForApiKeyAsync(int apiKeyId, DateTime revokedAt, CancellationToken cancellationToken);

        Task<int> PruneSpentAsync(DateTime now, CancellationToken cancellationToken);
    }
}
