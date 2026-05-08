namespace Lighthouse.Backend.Models.Auth
{
    public record CurrentUserProfileStatus
    {
        public string Subject { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public string? Email { get; init; }
    }
}