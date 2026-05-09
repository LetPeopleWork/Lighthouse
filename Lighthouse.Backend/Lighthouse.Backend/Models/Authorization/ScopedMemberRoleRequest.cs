namespace Lighthouse.Backend.Models.Authorization
{
    public record ScopedMemberRoleRequest
    {
        public string Role { get; init; } = string.Empty;
    }
}
