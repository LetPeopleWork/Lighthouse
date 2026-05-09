namespace Lighthouse.Backend.Models.Authorization
{
    public class RbacGroupMappingSummary
    {
        public int Id { get; set; }

        public string GroupValue { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public PermissionScopeType ScopeType { get; set; }

        public int? ScopeId { get; set; }
    }
}
