using Lighthouse.Backend.Models.Authorization;

namespace Lighthouse.Backend.Models.Auth
{
    // Issue-time superset check: when an instance of this DTO arrives on POST /api/apikeys,
    // the controller MUST validate that the caller's current effective permissions are a
    // superset of (Role, ScopeType, ScopeId) before persisting any ApiKeyPermission rows.
    // Between the check and the persist there is a small window where the caller's
    // permissions could be revoked by another admin. This is a documented residual risk
    // (see docs/compliance/cra-technical-file.md §5.3, "RBAC eventual consistency on
    // API-key issuance"). The window cannot be closed without globally serialising RBAC
    // mutations against key issuance; mitigation is post-hoc audit via the API-key list.
    public class ApiKeyScopeDto
    {
        public UserRole Role { get; set; }

        public PermissionScopeType ScopeType { get; set; }

        public int? ScopeId { get; set; }
    }
}
