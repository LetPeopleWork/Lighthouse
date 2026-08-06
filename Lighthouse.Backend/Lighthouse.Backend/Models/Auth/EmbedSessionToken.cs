using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.Auth
{
    // ADR-131: single use is a conditional update over these columns, not a read-then-write.
    public class EmbedSessionToken : IEntity
    {
        public int Id { get; set; }

        // ADR-137 D65: a row is either a grant (TokenId + SecretHash) or a refusal (RefusalCode),
        // never both — enforced by CK_EmbedSessionTokens_GrantOrRefusal, not by convention.
        public string? TokenId { get; set; }

        public string? SecretHash { get; set; }

        // ADR-137 D63: nothing mints against this any more — the column outlives the path because
        // migrations here are expand-only, and it goes with the D63 renames at the contract-phase
        // drop. `NamesAnIdentity` still reads it, so rows written before slice 03 stay redeemable.
        public int? ApiKeyId { get; set; }

        public string? Subject { get; set; }

        public string? HandshakeNonceHash { get; set; }

        public string? RefusalCode { get; set; }

        // ADR-137 D68 amends D55: consumption stamps this instead of clearing the hash, so a second
        // read of a consumed nonce stays findable and loggable (D62).
        public DateTime? HandshakeConsumedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? RedeemedAt { get; set; }

        public DateTime? RevokedAt { get; set; }
    }
}
