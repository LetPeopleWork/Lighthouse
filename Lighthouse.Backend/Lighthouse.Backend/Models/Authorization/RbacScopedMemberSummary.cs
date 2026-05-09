namespace Lighthouse.Backend.Models.Authorization
{
    public record RbacScopedMemberSummary
    {
        public int UserProfileId { get; init; }

        public string Subject { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public string? Email { get; init; }

        public UserRole? Role { get; init; }
    }
}
