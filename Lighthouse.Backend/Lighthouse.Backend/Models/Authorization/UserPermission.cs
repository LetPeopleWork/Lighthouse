using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.Authorization
{
    public class UserPermission : IEntity
    {
        public int Id { get; set; }

        public int UserProfileId { get; set; }

        public UserRole Role { get; set; }

        public PermissionScopeType ScopeType { get; set; }

        public int? ScopeId { get; set; }

        public int? GrantedByUserProfileId { get; set; }

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    }
}