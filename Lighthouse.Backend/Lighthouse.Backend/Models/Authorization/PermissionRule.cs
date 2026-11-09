namespace Lighthouse.Backend.Models.Authorization
{
    public readonly record struct PermissionRule(PermissionScopeType ScopeType, int? ScopeId, UserRole Role)
    {
        public bool HasSameScopeAs(PermissionRule other)
        {
            return ScopeType == other.ScopeType && ScopeId == other.ScopeId;
        }
    }
}
