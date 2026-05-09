using Lighthouse.Backend.Models.Authorization;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Interfaces.Authorization
{
    public interface IRbacAdministrationService
    {
        Task<RbacStatus> GetStatusAsync(CancellationToken cancellationToken = default);

        Task<bool> IsRbacEnforcedAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RbacUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<int>> GetReadableTeamIdsAsync(
            ClaimsPrincipal principal,
            IEnumerable<int> teamIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<int>> GetReadablePortfolioIdsAsync(
            ClaimsPrincipal principal,
            IEnumerable<int> portfolioIds,
            CancellationToken cancellationToken = default);

        Task<bool> CanReadTeamAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default);

        Task<bool> CanWriteTeamAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default);

        Task<bool> CanReadPortfolioAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default);

        Task<bool> CanWritePortfolioAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default);

        Task<bool> CanCreateTeamAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

        Task<bool> CanCreatePortfolioAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

        Task<bool> CanSatisfyRequirementAsync(
            ClaimsPrincipal principal,
            RbacGuardRequirement requirement,
            int? scopeId = null,
            CancellationToken cancellationToken = default);

        Task<UserAuthorizationSummary> GetAuthorizationSummaryAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> BootstrapCurrentUserAsSystemAdminAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> GrantSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> RevokeSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default);

        Task<bool> CanManageRbacAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

        Task<bool> CanManageTeamMembershipAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default);

        Task<bool> CanManagePortfolioMembershipAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RbacScopedMemberSummary>> GetTeamMembersAsync(int teamId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RbacScopedMemberSummary>> GetPortfolioMembersAsync(int portfolioId, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> SetTeamMemberRoleAsync(int userProfileId, int teamId, UserRole role, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> SetPortfolioMemberRoleAsync(int userProfileId, int portfolioId, UserRole role, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> RemoveTeamMemberAsync(int userProfileId, int teamId, CancellationToken cancellationToken = default);

        Task<RbacOperationResult> RemovePortfolioMemberAsync(int userProfileId, int portfolioId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RbacGroupMappingSummary>> GetGroupMappingsAsync(CancellationToken cancellationToken = default);

        Task<RbacOperationResult> CreateGroupMappingAsync(
            string groupValue,
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId,
            CancellationToken cancellationToken = default);

        Task<RbacOperationResult> RemoveGroupMappingAsync(int mappingId, CancellationToken cancellationToken = default);
    }
}