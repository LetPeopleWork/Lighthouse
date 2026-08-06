using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class EmbedSessionTokenRepository(LighthouseAppContext context) : IEmbedSessionTokenRepository
    {
        public async Task AddAsync(EmbedSessionToken token, CancellationToken cancellationToken)
        {
            await context.EmbedSessionTokens.AddAsync(token, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        public Task<EmbedSessionToken?> FindByTokenIdAsync(string tokenId, CancellationToken cancellationToken)
        {
            return context.EmbedSessionTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(token => token.TokenId == tokenId, cancellationToken);
        }

        public Task<int> TryMarkRedeemedAsync(string tokenId, DateTime redeemedAt, CancellationToken cancellationToken)
        {
            return context.EmbedSessionTokens
                .Where(token => token.TokenId == tokenId
                    && token.RedeemedAt == null
                    && token.RevokedAt == null
                    && token.ExpiresAt > redeemedAt)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(token => token.RedeemedAt, redeemedAt),
                    cancellationToken);
        }

        public Task<EmbedSessionToken?> FindByHandshakeNonceHashAsync(string nonceHash, CancellationToken cancellationToken)
        {
            return context.EmbedSessionTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(token => token.HandshakeNonceHash == nonceHash, cancellationToken);
        }

        public Task<int> TryConsumeHandshakeGrantAsync(
            string nonceHash,
            DateTime consumedAt,
            string secretHash,
            DateTime tokenExpiresAt,
            CancellationToken cancellationToken)
        {
            return UnconsumedOutcome(nonceHash, consumedAt)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(token => token.HandshakeConsumedAt, consumedAt)
                        .SetProperty(token => token.SecretHash, secretHash)
                        .SetProperty(token => token.ExpiresAt, tokenExpiresAt),
                    cancellationToken);
        }

        public Task<int> TryConsumeHandshakeRefusalAsync(string nonceHash, DateTime consumedAt, CancellationToken cancellationToken)
        {
            return UnconsumedOutcome(nonceHash, consumedAt)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(token => token.HandshakeConsumedAt, consumedAt),
                    cancellationToken);
        }

        public Task<int> RevokeOutstandingForApiKeyAsync(int apiKeyId, DateTime revokedAt, CancellationToken cancellationToken)
        {
            return context.EmbedSessionTokens
                .Where(token => token.ApiKeyId == apiKeyId
                    && token.RedeemedAt == null
                    && token.RevokedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(token => token.RevokedAt, revokedAt),
                    cancellationToken);
        }

        public Task<int> PruneSpentAsync(DateTime now, CancellationToken cancellationToken)
        {
            return context.EmbedSessionTokens
                .Where(token => token.ExpiresAt <= now
                    || token.RedeemedAt != null
                    || token.RevokedAt != null)
                .ExecuteDeleteAsync(cancellationToken);
        }

        // D68 keeps the nonce hash after consumption, so the row stays findable and the precondition
        // that decides the winner is HandshakeConsumedAt rather than the hash's absence.
        private IQueryable<EmbedSessionToken> UnconsumedOutcome(string nonceHash, DateTime consumedAt)
        {
            return context.EmbedSessionTokens
                .Where(token => token.HandshakeNonceHash == nonceHash
                    && token.HandshakeConsumedAt == null
                    && token.ExpiresAt > consumedAt);
        }
    }
}
