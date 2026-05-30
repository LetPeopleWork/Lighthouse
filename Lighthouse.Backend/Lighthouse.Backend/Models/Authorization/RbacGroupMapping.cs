using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.Authorization
{
    public class RbacGroupMapping : IConcurrencyTokenEntity
    {
        public int Id { get; set; }

        public Guid ConcurrencyToken { get; set; }

        public string GroupValue { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public PermissionScopeType ScopeType { get; set; }

        public int? ScopeId { get; set; }
    }
}
