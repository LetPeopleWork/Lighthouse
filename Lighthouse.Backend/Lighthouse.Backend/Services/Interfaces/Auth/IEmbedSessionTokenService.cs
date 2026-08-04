using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    // ADR-131. Redemption is exposed as one atomic operation on purpose: a Find next to a
    // MarkRedeemed would offer callers a way to lose the race.
    public interface IEmbedSessionTokenService
    {
        Task<EmbedSessionTokenMintResult> MintAsync(int apiKeyId, CancellationToken cancellationToken);

        Task<EmbedSessionTokenRedemption> RedeemAsync(string? token, CancellationToken cancellationToken);

        Task RevokeAllAsync(int apiKeyId, CancellationToken cancellationToken);
    }
}
