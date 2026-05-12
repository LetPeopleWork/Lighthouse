using System.Security.Claims;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public sealed class ClaimsDrivenRbacAdministrationService : IRbacAdministrationService
    {
        public const string GrantClaimType = "rbac_grant";
        public const string SystemAdminGrant = "SystemAdmin";
        public const string TeamAdminGrantPrefix = "TeamAdmin:";
        public const string PortfolioAdminGrantPrefix = "PortfolioAdmin:";
        public const string ViewerTeamGrantPrefix = "ViewerTeam:";
        public const string ViewerPortfolioGrantPrefix = "ViewerPortfolio:";

        public Task<bool> CanSatisfyRequirementAsync(
            ClaimsPrincipal principal,
            RbacGuardRequirement requirement,
            int? scopeId = null,
            CancellationToken cancellationToken = default)
        {
            var grants = ExtractGrants(principal);
            var isSystemAdmin = grants.Contains(SystemAdminGrant);
            var hasAnyScopedAdminGrant = grants.Any(g =>
                g.StartsWith(TeamAdminGrantPrefix, StringComparison.Ordinal)
                || g.StartsWith(PortfolioAdminGrantPrefix, StringComparison.Ordinal));

            return Task.FromResult(requirement switch
            {
                RbacGuardRequirement.SystemAdmin => isSystemAdmin,
                RbacGuardRequirement.SystemAdminOrBootstrap => isSystemAdmin,
                RbacGuardRequirement.AnyScopedAdmin => isSystemAdmin || hasAnyScopedAdminGrant,
                RbacGuardRequirement.TeamRead => isSystemAdmin
                    || (scopeId.HasValue
                        && (grants.Contains($"{TeamAdminGrantPrefix}{scopeId.Value}")
                            || grants.Contains($"{ViewerTeamGrantPrefix}{scopeId.Value}"))),
                RbacGuardRequirement.TeamWrite => isSystemAdmin
                    || (scopeId.HasValue && grants.Contains($"{TeamAdminGrantPrefix}{scopeId.Value}")),
                RbacGuardRequirement.PortfolioRead => isSystemAdmin
                    || (scopeId.HasValue
                        && (grants.Contains($"{PortfolioAdminGrantPrefix}{scopeId.Value}")
                            || grants.Contains($"{ViewerPortfolioGrantPrefix}{scopeId.Value}"))),
                RbacGuardRequirement.PortfolioWrite => isSystemAdmin
                    || (scopeId.HasValue && grants.Contains($"{PortfolioAdminGrantPrefix}{scopeId.Value}")),
                RbacGuardRequirement.CanCreateTeam => isSystemAdmin,
                RbacGuardRequirement.CanCreatePortfolio => isSystemAdmin,
                _ => false,
            });
        }

        public Task<bool> IsRbacEnforcedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> CanManageRbacAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            => Task.FromResult(ExtractGrants(principal).Contains(SystemAdminGrant));

        public Task<RbacStatus> GetStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new RbacStatus
            {
                Enabled = true,
                PremiumGateSatisfied = true,
                HasSystemAdmin = true,
                ReadyForEnablement = true,
            });

        public Task<IReadOnlyList<RbacUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RbacUserSummary>>([]);

        public Task<IReadOnlyList<int>> GetReadableTeamIdsAsync(
            ClaimsPrincipal principal,
            IEnumerable<int> teamIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(teamIds.ToList());

        public Task<IReadOnlyList<int>> GetReadablePortfolioIdsAsync(
            ClaimsPrincipal principal,
            IEnumerable<int> portfolioIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(portfolioIds.ToList());

        public Task<bool> CanReadTeamAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default)
            => CanSatisfyRequirementAsync(principal, RbacGuardRequirement.TeamRead, teamId, cancellationToken);

        public Task<bool> CanWriteTeamAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default)
            => CanSatisfyRequirementAsync(principal, RbacGuardRequirement.TeamWrite, teamId, cancellationToken);

        public Task<bool> CanReadPortfolioAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default)
            => CanSatisfyRequirementAsync(principal, RbacGuardRequirement.PortfolioRead, portfolioId, cancellationToken);

        public Task<bool> CanWritePortfolioAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default)
            => CanSatisfyRequirementAsync(principal, RbacGuardRequirement.PortfolioWrite, portfolioId, cancellationToken);

        public Task<bool> CanCreateTeamAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            => CanManageRbacAsync(principal, cancellationToken);

        public Task<bool> CanCreatePortfolioAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            => CanManageRbacAsync(principal, cancellationToken);

        public Task<UserAuthorizationSummary> GetAuthorizationSummaryAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            => Task.FromResult(new UserAuthorizationSummary { IsRbacEnabled = true, SystemAdminDisplayNames = [] });

        public Task<RbacOperationResult> BootstrapCurrentUserAsSystemAdminAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> GrantSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> RevokeSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> DeleteUserAsync(int userProfileId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<bool> CanManageTeamMembershipAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default)
            => CanWriteTeamAsync(principal, teamId, cancellationToken);

        public Task<bool> CanManagePortfolioMembershipAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default)
            => CanWritePortfolioAsync(principal, portfolioId, cancellationToken);

        public Task<IReadOnlyList<RbacScopedMemberSummary>> GetTeamMembersAsync(int teamId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RbacScopedMemberSummary>>([]);

        public Task<IReadOnlyList<RbacScopedMemberSummary>> GetPortfolioMembersAsync(int portfolioId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RbacScopedMemberSummary>>([]);

        public Task<RbacOperationResult> SetTeamMemberRoleAsync(int userProfileId, int teamId, UserRole role, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> SetPortfolioMemberRoleAsync(int userProfileId, int portfolioId, UserRole role, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> RemoveTeamMemberAsync(int userProfileId, int teamId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> RemovePortfolioMemberAsync(int userProfileId, int portfolioId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<IReadOnlyList<RbacGroupMappingSummary>> GetGroupMappingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RbacGroupMappingSummary>>([]);

        public Task<IReadOnlyList<RbacGroupMappingSummary>> GetTeamGroupMappingsAsync(int teamId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RbacGroupMappingSummary>>([]);

        public Task<IReadOnlyList<RbacGroupMappingSummary>> GetPortfolioGroupMappingsAsync(int portfolioId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RbacGroupMappingSummary>>([]);

        public Task<RbacOperationResult> CreateGroupMappingAsync(string groupValue, UserRole role, PermissionScopeType scopeType, int? scopeId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> RemoveGroupMappingAsync(int mappingId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> GrantCreatorTeamAdminAsync(int userProfileId, int teamId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task<RbacOperationResult> GrantCreatorPortfolioAdminAsync(int userProfileId, int portfolioId, CancellationToken cancellationToken = default)
            => Task.FromResult(RbacOperationResult.Success());

        public Task EnsureCreatorTeamAdminAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task EnsureCreatorPortfolioAdminAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        private static HashSet<string> ExtractGrants(ClaimsPrincipal principal)
            => principal.FindAll(GrantClaimType).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
    }
}
