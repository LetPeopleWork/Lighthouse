namespace Lighthouse.Backend.Models.Authorization
{
    public record RbacUserSummary
    {
        public int Id { get; init; }

        public string Subject { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public string? Email { get; init; }

        public bool IsSystemAdmin { get; init; }
    }
}