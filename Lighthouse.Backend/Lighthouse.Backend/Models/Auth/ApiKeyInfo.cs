namespace Lighthouse.Backend.Models.Auth
{
    public class ApiKeyInfo
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? LastUsedAt { get; set; }

        public IReadOnlyList<ApiKeyScopeDto> Scopes { get; set; } = Array.Empty<ApiKeyScopeDto>();
    }
}
