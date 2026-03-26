namespace Lighthouse.Backend.Models.Auth
{
    public record AuthSessionStatus
    {
        public bool IsAuthenticated { get; init; }

        public string? DisplayName { get; init; }

        public string? Email { get; init; }
    }
}
