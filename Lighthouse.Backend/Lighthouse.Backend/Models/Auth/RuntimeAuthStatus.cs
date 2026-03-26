namespace Lighthouse.Backend.Models.Auth
{
    public record RuntimeAuthStatus
    {
        public AuthMode Mode { get; init; }

        public string? MisconfigurationMessage { get; init; }
    }
}
