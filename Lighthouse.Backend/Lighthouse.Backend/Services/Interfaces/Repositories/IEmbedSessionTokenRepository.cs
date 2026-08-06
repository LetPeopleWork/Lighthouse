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

        Task<EmbedSessionToken?> FindByHandshakeNonceHashAsync(string nonceHash, CancellationToken cancellationToken);

        /// <summary>
        /// ADR-132 D68/DQ-2: stamps the outcome consumed only if nobody has consumed it yet, and in the
        /// same statement writes the secret the poll just minted (D71) and swaps the outcome window for
        /// the token window redemption enforces. Racing pollers see exactly one 1.
        /// </summary>
        Task<int> TryConsumeHandshakeGrantAsync(
            string nonceHash,
            DateTime consumedAt,
            string secretHash,
            DateTime tokenExpiresAt,
            CancellationToken cancellationToken);

        Task<int> TryConsumeHandshakeRefusalAsync(string nonceHash, DateTime consumedAt, CancellationToken cancellationToken);

        Task<int> RevokeOutstandingForApiKeyAsync(int apiKeyId, DateTime revokedAt, CancellationToken cancellationToken);

        Task<int> PruneSpentAsync(DateTime now, CancellationToken cancellationToken);
    }
}
