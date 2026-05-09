using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    public class RbacAdministrationService(
        LighthouseAppContext context,
        IOptions<AuthorizationConfiguration> configuration,
        ILicenseService licenseService,
        ICurrentUserProfileService currentUserProfileService,
        ILogger<RbacAdministrationService> logger) : IRbacAdministrationService
    {
        public async Task<RbacStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var hasSystemAdmin = await HasSystemAdminAsync(cancellationToken);
            var premiumGateSatisfied = licenseService.CanUsePremiumFeatures();
            var config = configuration.Value;
            var unassignedUserCount = await GetUnassignedUserCountAsync(cancellationToken);

            return new RbacStatus
            {
                Enabled = config.Enabled,
                PremiumGateSatisfied = premiumGateSatisfied,
                HasSystemAdmin = hasSystemAdmin,
                HasEmergencyAdminConfigured = config.EmergencySystemAdminSubjects.Count > 0,
                ReadyForEnablement = premiumGateSatisfied && hasSystemAdmin,
                UnassignedUserCount = unassignedUserCount,
            };
        }

        public Task<bool> IsRbacEnforcedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(configuration.Value.Enabled);
        }

        public async Task<IReadOnlyList<RbacUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            var systemAdminIds = await context.UserPermissions
                .Where(p => p.ScopeType == PermissionScopeType.System && p.Role == UserRole.SystemAdmin)
                .Select(p => p.UserProfileId)
                .ToListAsync(cancellationToken);

            var usersWithAnyAssignment = await context.UserPermissions
                .Select(p => p.UserProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var systemAdminIdSet = systemAdminIds.ToHashSet();
            var assignedUserIdSet = usersWithAnyAssignment.ToHashSet();

            var users = await context.UserProfiles
                .OrderBy(p => p.DisplayName)
                .ThenBy(p => p.Email)
                .Select(p => new RbacUserSummary
                {
                    Id = p.Id,
                    Subject = p.Subject,
                    DisplayName = p.DisplayName,
                    Email = p.Email,
                    IsSystemAdmin = systemAdminIdSet.Contains(p.Id),
                    IsUnassigned = !assignedUserIdSet.Contains(p.Id),
                })
                .ToListAsync(cancellationToken);

            return users;
        }

        public async Task<IReadOnlyList<int>> GetReadableTeamIdsAsync(
            ClaimsPrincipal principal,
            IEnumerable<int> teamIds,
            CancellationToken cancellationToken = default)
        {
            var distinctTeamIds = teamIds.Distinct().ToArray();

            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return distinctTeamIds;
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return [];
            }

            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return distinctTeamIds;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return [];
            }

            var readableTeamIds = await context.UserPermissions
                .Where(p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Team
                    && p.ScopeId.HasValue
                    && distinctTeamIds.Contains(p.ScopeId.Value)
                    && (p.Role == UserRole.TeamAdmin || p.Role == UserRole.Viewer))
                .Select(p => p.ScopeId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            return readableTeamIds;
        }

        public async Task<IReadOnlyList<int>> GetReadablePortfolioIdsAsync(
            ClaimsPrincipal principal,
            IEnumerable<int> portfolioIds,
            CancellationToken cancellationToken = default)
        {
            var distinctPortfolioIds = portfolioIds.Distinct().ToArray();

            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return distinctPortfolioIds;
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return [];
            }

            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return distinctPortfolioIds;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return [];
            }

            var readablePortfolioIds = await context.UserPermissions
                .Where(p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Portfolio
                    && p.ScopeId.HasValue
                    && distinctPortfolioIds.Contains(p.ScopeId.Value)
                    && (p.Role == UserRole.PortfolioAdmin || p.Role == UserRole.Viewer))
                .Select(p => p.ScopeId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            return readablePortfolioIds;
        }

        public async Task<bool> CanReadTeamAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return true;
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return false;
            }

            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return true;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Team
                    && p.ScopeId == teamId
                    && (p.Role == UserRole.TeamAdmin || p.Role == UserRole.Viewer),
                cancellationToken);
        }

        public async Task<bool> CanWriteTeamAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return true;
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return false;
            }

            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return true;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Team
                    && p.ScopeId == teamId
                    && p.Role == UserRole.TeamAdmin,
                cancellationToken);
        }

        public async Task<bool> CanReadPortfolioAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return true;
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return false;
            }

            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return true;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Portfolio
                    && p.ScopeId == portfolioId
                    && (p.Role == UserRole.PortfolioAdmin || p.Role == UserRole.Viewer),
                cancellationToken);
        }

        public async Task<bool> CanWritePortfolioAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return true;
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return false;
            }

            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return true;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Portfolio
                    && p.ScopeId == portfolioId
                    && p.Role == UserRole.PortfolioAdmin,
                cancellationToken);
        }

        public async Task<bool> CanCreateTeamAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return true;
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return false;
            }

            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return true;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Team
                    && p.Role == UserRole.TeamAdmin,
                cancellationToken);
        }

        public async Task<bool> CanCreatePortfolioAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return true;
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return false;
            }

            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return true;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Portfolio
                    && p.Role == UserRole.PortfolioAdmin,
                cancellationToken);
        }

        public async Task<bool> CanSatisfyRequirementAsync(
            ClaimsPrincipal principal,
            RbacGuardRequirement requirement,
            int? scopeId = null,
            CancellationToken cancellationToken = default)
        {
            return requirement switch
            {
                RbacGuardRequirement.SystemAdmin => !await IsRbacEnforcedAsync(cancellationToken)
                    || await CanManageRbacAsync(principal, cancellationToken),
                RbacGuardRequirement.TeamRead => scopeId.HasValue
                    && await CanReadTeamAsync(principal, scopeId.Value, cancellationToken),
                RbacGuardRequirement.TeamWrite => scopeId.HasValue
                    && await CanWriteTeamAsync(principal, scopeId.Value, cancellationToken),
                RbacGuardRequirement.PortfolioRead => scopeId.HasValue
                    && await CanReadPortfolioAsync(principal, scopeId.Value, cancellationToken),
                RbacGuardRequirement.PortfolioWrite => scopeId.HasValue
                    && await CanWritePortfolioAsync(principal, scopeId.Value, cancellationToken),
                _ => false,
            };
        }

        public async Task<UserAuthorizationSummary> GetAuthorizationSummaryAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var isRbacEnabled = await IsRbacEnforcedAsync(cancellationToken);

            if (!isRbacEnabled)
            {
                return new UserAuthorizationSummary
                {
                    IsRbacEnabled = false,
                    IsSystemAdmin = true,
                    CanCreateTeam = true,
                    CanCreatePortfolio = true,
                    SystemAdminDisplayNames = [],
                };
            }

            var isSystemAdmin = await CanManageRbacAsync(principal, cancellationToken);
            var canCreateTeam = isSystemAdmin || await CanCreateTeamAsync(principal, cancellationToken);
            var canCreatePortfolio = isSystemAdmin || await CanCreatePortfolioAsync(principal, cancellationToken);
            var systemAdminDisplayNames = await GetSystemAdminDisplayNamesAsync(cancellationToken);

            return new UserAuthorizationSummary
            {
                IsRbacEnabled = true,
                IsSystemAdmin = isSystemAdmin,
                CanCreateTeam = canCreateTeam,
                CanCreatePortfolio = canCreatePortfolio,
                SystemAdminDisplayNames = systemAdminDisplayNames,
            };
        }

        public async Task<RbacOperationResult> BootstrapCurrentUserAsSystemAdminAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            if (await HasSystemAdminAsync(cancellationToken))
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.AlreadyBootstrapped,
                    "A System Admin already exists.");
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                logger.LogWarning("RBAC bootstrap rejected due to missing stable subject claim");
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.MissingStableSubject,
                    "Current user does not contain a stable subject claim.");
            }

            var result = await GrantSystemAdminAsync(currentUser.Id, cancellationToken);
            return result;
        }

        public async Task<RbacOperationResult> GrantSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default)
        {
            var userExists = await context.UserProfiles
                .AnyAsync(p => p.Id == userProfileId, cancellationToken);

            if (!userExists)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.UserNotFound,
                    "User profile was not found.");
            }

            var existingPermission = await context.UserPermissions
                .AnyAsync(
                    p => p.UserProfileId == userProfileId
                        && p.ScopeType == PermissionScopeType.System
                        && p.Role == UserRole.SystemAdmin,
                    cancellationToken);

            if (existingPermission)
            {
                return RbacOperationResult.Success();
            }

            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = userProfileId,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
                Role = UserRole.SystemAdmin,
                GrantedAt = DateTime.UtcNow,
            });

            await context.SaveChangesAsync(cancellationToken);
            return RbacOperationResult.Success();
        }

        public async Task<RbacOperationResult> RevokeSystemAdminAsync(int userProfileId, CancellationToken cancellationToken = default)
        {
            var permission = await context.UserPermissions
                .SingleOrDefaultAsync(
                    p => p.UserProfileId == userProfileId
                        && p.ScopeType == PermissionScopeType.System
                        && p.Role == UserRole.SystemAdmin,
                    cancellationToken);

            if (permission is null)
            {
                return RbacOperationResult.Success();
            }

            var systemAdminCount = await context.UserPermissions
                .CountAsync(
                    p => p.ScopeType == PermissionScopeType.System && p.Role == UserRole.SystemAdmin,
                    cancellationToken);

            if (systemAdminCount <= 1)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.LastSystemAdmin,
                    "Cannot remove the last System Admin.");
            }

            context.UserPermissions.Remove(permission);
            await context.SaveChangesAsync(cancellationToken);
            return RbacOperationResult.Success();
        }

        public async Task<bool> CanManageRbacAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            var emergencySubjects = configuration.Value.EmergencySystemAdminSubjects;
            var isEmergencySystemAdmin = emergencySubjects
                .Any(subject => string.Equals(subject, currentUser.Subject, StringComparison.Ordinal));
            if (isEmergencySystemAdmin)
            {
                return true;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.System
                    && p.Role == UserRole.SystemAdmin,
                cancellationToken);
        }

        public async Task<bool> CanManageTeamMembershipAsync(ClaimsPrincipal principal, int teamId, CancellationToken cancellationToken = default)
        {
            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return true;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Team
                    && p.ScopeId == teamId
                    && p.Role == UserRole.TeamAdmin,
                cancellationToken);
        }

        public async Task<bool> CanManagePortfolioMembershipAsync(ClaimsPrincipal principal, int portfolioId, CancellationToken cancellationToken = default)
        {
            if (await CanManageRbacAsync(principal, cancellationToken))
            {
                return true;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return false;
            }

            return await context.UserPermissions.AnyAsync(
                p => p.UserProfileId == currentUser.Id
                    && p.ScopeType == PermissionScopeType.Portfolio
                    && p.ScopeId == portfolioId
                    && p.Role == UserRole.PortfolioAdmin,
                cancellationToken);
        }

        public async Task<IReadOnlyList<RbacScopedMemberSummary>> GetTeamMembersAsync(int teamId, CancellationToken cancellationToken = default)
        {
            var roleLookup = await context.UserPermissions
                .Where(p => p.ScopeType == PermissionScopeType.Team
                    && p.ScopeId == teamId
                    && (p.Role == UserRole.TeamAdmin || p.Role == UserRole.Viewer))
                .ToListAsync(cancellationToken);

            var roleByUser = roleLookup
                .GroupBy(x => x.UserProfileId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Any(p => p.Role == UserRole.TeamAdmin) ? UserRole.TeamAdmin : UserRole.Viewer);

            var users = await context.UserProfiles
                .OrderBy(x => x.DisplayName)
                .ThenBy(x => x.Email)
                .Select(x => new RbacScopedMemberSummary
                {
                    UserProfileId = x.Id,
                    Subject = x.Subject,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Role = roleByUser.ContainsKey(x.Id) ? roleByUser[x.Id] : null,
                })
                .ToListAsync(cancellationToken);

            return users;
        }

        public async Task<IReadOnlyList<RbacScopedMemberSummary>> GetPortfolioMembersAsync(int portfolioId, CancellationToken cancellationToken = default)
        {
            var roleLookup = await context.UserPermissions
                .Where(p => p.ScopeType == PermissionScopeType.Portfolio
                    && p.ScopeId == portfolioId
                    && (p.Role == UserRole.PortfolioAdmin || p.Role == UserRole.Viewer))
                .ToListAsync(cancellationToken);

            var roleByUser = roleLookup
                .GroupBy(x => x.UserProfileId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Any(p => p.Role == UserRole.PortfolioAdmin) ? UserRole.PortfolioAdmin : UserRole.Viewer);

            var users = await context.UserProfiles
                .OrderBy(x => x.DisplayName)
                .ThenBy(x => x.Email)
                .Select(x => new RbacScopedMemberSummary
                {
                    UserProfileId = x.Id,
                    Subject = x.Subject,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Role = roleByUser.ContainsKey(x.Id) ? roleByUser[x.Id] : null,
                })
                .ToListAsync(cancellationToken);

            return users;
        }

        public async Task<RbacOperationResult> SetTeamMemberRoleAsync(int userProfileId, int teamId, UserRole role, CancellationToken cancellationToken = default)
        {
            if (role != UserRole.TeamAdmin && role != UserRole.Viewer)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.InvalidRoleForScope,
                    "Role is invalid for team scope.");
            }

            var userExists = await context.UserProfiles.AnyAsync(x => x.Id == userProfileId, cancellationToken);
            if (!userExists)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.UserNotFound,
                    "User profile was not found.");
            }

            var currentPermissions = await context.UserPermissions
                .Where(x => x.UserProfileId == userProfileId
                    && x.ScopeType == PermissionScopeType.Team
                    && x.ScopeId == teamId
                    && (x.Role == UserRole.TeamAdmin || x.Role == UserRole.Viewer))
                .ToListAsync(cancellationToken);

            var permissionsToRemove = currentPermissions.Where(x => x.Role != role).ToList();
            if (permissionsToRemove.Count > 0)
            {
                context.UserPermissions.RemoveRange(permissionsToRemove);
            }

            if (!currentPermissions.Any(x => x.Role == role))
            {
                context.UserPermissions.Add(new UserPermission
                {
                    UserProfileId = userProfileId,
                    ScopeType = PermissionScopeType.Team,
                    ScopeId = teamId,
                    Role = role,
                    GrantedAt = DateTime.UtcNow,
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            return RbacOperationResult.Success();
        }

        public async Task<RbacOperationResult> SetPortfolioMemberRoleAsync(int userProfileId, int portfolioId, UserRole role, CancellationToken cancellationToken = default)
        {
            if (role != UserRole.PortfolioAdmin && role != UserRole.Viewer)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.InvalidRoleForScope,
                    "Role is invalid for portfolio scope.");
            }

            var userExists = await context.UserProfiles.AnyAsync(x => x.Id == userProfileId, cancellationToken);
            if (!userExists)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.UserNotFound,
                    "User profile was not found.");
            }

            var currentPermissions = await context.UserPermissions
                .Where(x => x.UserProfileId == userProfileId
                    && x.ScopeType == PermissionScopeType.Portfolio
                    && x.ScopeId == portfolioId
                    && (x.Role == UserRole.PortfolioAdmin || x.Role == UserRole.Viewer))
                .ToListAsync(cancellationToken);

            var permissionsToRemove = currentPermissions.Where(x => x.Role != role).ToList();
            if (permissionsToRemove.Count > 0)
            {
                context.UserPermissions.RemoveRange(permissionsToRemove);
            }

            if (!currentPermissions.Any(x => x.Role == role))
            {
                context.UserPermissions.Add(new UserPermission
                {
                    UserProfileId = userProfileId,
                    ScopeType = PermissionScopeType.Portfolio,
                    ScopeId = portfolioId,
                    Role = role,
                    GrantedAt = DateTime.UtcNow,
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            return RbacOperationResult.Success();
        }

        public async Task<RbacOperationResult> RemoveTeamMemberAsync(int userProfileId, int teamId, CancellationToken cancellationToken = default)
        {
            var memberships = await context.UserPermissions
                .Where(x => x.UserProfileId == userProfileId
                    && x.ScopeType == PermissionScopeType.Team
                    && x.ScopeId == teamId
                    && (x.Role == UserRole.TeamAdmin || x.Role == UserRole.Viewer))
                .ToListAsync(cancellationToken);

            if (memberships.Count == 0)
            {
                return RbacOperationResult.Success();
            }

            context.UserPermissions.RemoveRange(memberships);
            await context.SaveChangesAsync(cancellationToken);
            return RbacOperationResult.Success();
        }

        public async Task<RbacOperationResult> RemovePortfolioMemberAsync(int userProfileId, int portfolioId, CancellationToken cancellationToken = default)
        {
            var memberships = await context.UserPermissions
                .Where(x => x.UserProfileId == userProfileId
                    && x.ScopeType == PermissionScopeType.Portfolio
                    && x.ScopeId == portfolioId
                    && (x.Role == UserRole.PortfolioAdmin || x.Role == UserRole.Viewer))
                .ToListAsync(cancellationToken);

            if (memberships.Count == 0)
            {
                return RbacOperationResult.Success();
            }

            context.UserPermissions.RemoveRange(memberships);
            await context.SaveChangesAsync(cancellationToken);
            return RbacOperationResult.Success();
        }

        private Task<bool> HasSystemAdminAsync(CancellationToken cancellationToken)
        {
            return context.UserPermissions.AnyAsync(
                p => p.ScopeType == PermissionScopeType.System && p.Role == UserRole.SystemAdmin,
                cancellationToken);
        }

        private Task<int> GetUnassignedUserCountAsync(CancellationToken cancellationToken)
        {
            return context.UserProfiles
                .CountAsync(
                    profile => !context.UserPermissions.Any(permission => permission.UserProfileId == profile.Id),
                    cancellationToken);
        }

        private async Task<IReadOnlyList<string>> GetSystemAdminDisplayNamesAsync(CancellationToken cancellationToken)
        {
            var identityRows = await context.UserPermissions
                .Where(permission => permission.ScopeType == PermissionScopeType.System && permission.Role == UserRole.SystemAdmin)
                .Join(
                    context.UserProfiles,
                    permission => permission.UserProfileId,
                    profile => profile.Id,
                    (_, profile) => new { profile.DisplayName })
                .ToListAsync(cancellationToken);

            return identityRows
                .Select(x => string.IsNullOrWhiteSpace(x.DisplayName) ? "System Admin" : x.DisplayName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
        }

        private async Task<bool> IsEnforcementGateSatisfiedAsync(CancellationToken cancellationToken)
        {
            if (!licenseService.CanUsePremiumFeatures())
            {
                return false;
            }

            return await HasSystemAdminAsync(cancellationToken);
        }
    }
}