using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    // ADR-131. Redemption is exposed as one atomic operation on purpose: a Find next to a
    // MarkRedeemed would offer callers a way to lose the race.
    public interface IEmbedSessionTokenService
    {
        Task<EmbedSessionTokenMintResult> MintAsync(int apiKeyId, CancellationToken cancellationToken);

        /// <summary>
        /// ADR-137 D51/D54: the outcome of a sign-in hop is recorded once, at resolution. A grant and
        /// a refusal are separate operations because a row can only ever be one of the two
        /// (CK_EmbedSessionTokens_GrantOrRefusal), and a bool parameter would let a caller ask for both.
        /// </summary>
        Task RecordHandshakeGrantAsync(string? subject, string nonce, CancellationToken cancellationToken);

        Task RecordHandshakeRefusalAsync(string? subject, string nonce, string refusalCode, CancellationToken cancellationToken);

        /// <summary>
        /// ADR-137 D68: reads a recorded outcome back exactly once. Unknown, unresolved, expired and
        /// already-consumed nonces all answer <see cref="EmbedHandshakeOutcome.Unresolved"/>.
        /// </summary>
        Task<EmbedHandshakeOutcome> ConsumeHandshakeAsync(string? nonce, CancellationToken cancellationToken);

        Task<EmbedSessionTokenRedemption> RedeemAsync(string? token, CancellationToken cancellationToken);

        Task RevokeAllAsync(int apiKeyId, CancellationToken cancellationToken);
    }

    public sealed record EmbedHandshakeOutcome(string? Token, DateTime? ExpiresAt, string? RefusalCode)
    {
        public static EmbedHandshakeOutcome Unresolved { get; } = new(null, null, null);
    }
}
