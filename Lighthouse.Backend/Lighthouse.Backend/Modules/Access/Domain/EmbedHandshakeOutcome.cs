namespace Lighthouse.Backend.Modules.Access.Domain
{
    public sealed record EmbedHandshakeOutcome(string? Token, DateTime? ExpiresAt, string? RefusalCode)
    {
        public static EmbedHandshakeOutcome Unresolved { get; } = new(null, null, null);
    }
}
