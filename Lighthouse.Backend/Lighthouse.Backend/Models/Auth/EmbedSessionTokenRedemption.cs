namespace Lighthouse.Backend.Models.Auth
{
    // ADR-137 D63: a redeemed row names either an API key or a viewer, never both, so the caller
    // branches on which one arrived rather than on a sentinel.
    public readonly record struct EmbedSessionTokenRedemption(bool Succeeded, int? ApiKeyId, string? Subject)
    {
        // Only Succeeded is readable on a refusal; the other two carry nothing a caller may act on.
        public static EmbedSessionTokenRedemption Refused => new(false, 0, null);
    }
}
