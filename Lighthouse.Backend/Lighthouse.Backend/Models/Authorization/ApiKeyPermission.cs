using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.Authorization
{
    public class ApiKeyPermission : IEntity
    {
        public int Id { get; set; }

        public int ApiKeyId { get; set; }

        public UserRole Role { get; set; }

        public PermissionScopeType ScopeType { get; set; }

        public int? ScopeId { get; set; }

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    }
}
