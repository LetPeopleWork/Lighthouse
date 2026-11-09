using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    public class RbacAdministrationService(
        LighthouseAppContext context,
        IOptions<AuthorizationConfiguration> configuration,
        ILicenseService licenseService,
        ICurrentUserProfileService currentUserProfileService,
        ILogger<RbacAdministrationService> logger,
        TimeProvider timeProvider) : IRbacAdministrationService
    {

        private const string UserProfileString = "User profile";

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
                GroupClaimName = string.IsNullOrWhiteSpace(config.GroupClaimName) ? null : config.GroupClaimName,
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
            var emergencySubjects = configuration.Value.EmergencySystemAdminSubjects;

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

            return users
                .Select(u => u with
                {
                    IsEmergencyAdmin = emergencySubjects.Any(s => string.Equals(s, u.Subject, StringComparison.Ordinal)),
                })
                .ToList();
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);

            return distinctTeamIds
                .Where(teamId => effectiveAccess.CanRead(PermissionScopeType.Team, teamId))
                .ToList();
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);

            return distinctPortfolioIds
                .Where(portfolioId => effectiveAccess.CanRead(PermissionScopeType.Portfolio, portfolioId))
                .ToList();
        }

        /// <summary>
        /// The write twin of <see cref="GetReadablePortfolioIdsAsync"/>, kept branch-for-branch identical with the
        /// write check swapped in: any other difference between the two silently grants more than it should.
        /// </summary>
        public async Task<IReadOnlyList<int>> GetWritablePortfolioIdsAsync(
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);

            return distinctPortfolioIds
                .Where(portfolioId => effectiveAccess.CanWrite(PermissionScopeType.Portfolio, portfolioId))
                .ToList();
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);
            return effectiveAccess.CanRead(PermissionScopeType.Team, teamId);
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);
            return effectiveAccess.CanWrite(PermissionScopeType.Team, teamId);
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);
            return effectiveAccess.CanRead(PermissionScopeType.Portfolio, portfolioId);
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);
            return effectiveAccess.CanWrite(PermissionScopeType.Portfolio, portfolioId);
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

            return await CanManageRbacAsync(principal, cancellationToken);
        }

        public async Task<bool> CanCreatePortfolioAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return await HasAnyTeamAsync(cancellationToken);
            }

            if (!await IsEnforcementGateSatisfiedAsync(cancellationToken))
            {
                return false;
            }

            if (!await CanManageRbacAsync(principal, cancellationToken))
            {
                return false;
            }

            return await HasAnyTeamAsync(cancellationToken);
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
                RbacGuardRequirement.CanCreateTeam => await CanCreateTeamAsync(principal, cancellationToken),
                RbacGuardRequirement.CanCreatePortfolio => await CanCreatePortfolioAsync(principal, cancellationToken),
                RbacGuardRequirement.CanDeleteUnit => await CanDeleteUnitAsync(principal, cancellationToken),
                RbacGuardRequirement.SystemAdminOrBootstrap => !await IsRbacEnforcedAsync(cancellationToken)
                    || !await HasSystemAdminAsync(cancellationToken)
                    || await CanManageRbacAsync(principal, cancellationToken),
                RbacGuardRequirement.AnyScopedAdmin => await CanSatisfyAnyScopedAdminAsync(principal, cancellationToken),
                RbacGuardRequirement.TeamContribute => scopeId.HasValue
                    && await CanReadTeamAsync(principal, scopeId.Value, cancellationToken),
                _ => false,
            };
        }

        // Deleting has its own rule so a change to who may create cannot quietly change who may delete.
        private async Task<bool> CanDeleteUnitAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return true;
            }

            return await IsEnforcementGateSatisfiedAsync(cancellationToken)
                && await CanManageRbacAsync(principal, cancellationToken);
        }

        private async Task<bool> CanSatisfyAnyScopedAdminAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken)
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);

            return effectiveAccess.AdministersAnyUnit;
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
                    CanCreatePortfolio = await HasAnyTeamAsync(cancellationToken),
                    SystemAdminDisplayNames = [],
                };
            }

            // Bootstrap mode: when RBAC is enabled but no system admin has been configured yet,
            // any authenticated user is treated as system admin so they can access the RBAC
            // settings page and perform the initial bootstrap.
            var hasSystemAdmin = await HasSystemAdminAsync(cancellationToken);
            if (!hasSystemAdmin)
            {
                return new UserAuthorizationSummary
                {
                    IsRbacEnabled = true,
                    IsSystemAdmin = true,
                    CanCreateTeam = true,
                    CanCreatePortfolio = await HasAnyTeamAsync(cancellationToken),
                    SystemAdminDisplayNames = [],
                };
            }

            var isSystemAdmin = await CanManageRbacAsync(principal, cancellationToken);
            var canCreateTeam = await CanCreateTeamAsync(principal, cancellationToken);
            var canCreatePortfolio = await CanCreatePortfolioAsync(principal, cancellationToken);
            var systemAdminDisplayNames = await GetSystemAdminDisplayNamesAsync(cancellationToken);

            IReadOnlyList<int> adminTeamIds = [];
            IReadOnlyList<int> adminPortfolioIds = [];

            if (!isSystemAdmin
                && await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken) is { } currentUser)
            {
                var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);
                var storedTeamIds = await context.Teams.Select(team => team.Id).ToListAsync(cancellationToken);
                var storedPortfolioIds = await context.Portfolios.Select(portfolio => portfolio.Id).ToListAsync(cancellationToken);
                adminTeamIds = effectiveAccess.AdministeredUnitIds(PermissionScopeType.Team, storedTeamIds);
                adminPortfolioIds = effectiveAccess.AdministeredUnitIds(PermissionScopeType.Portfolio, storedPortfolioIds);
            }

            return new UserAuthorizationSummary
            {
                IsRbacEnabled = true,
                IsSystemAdmin = isSystemAdmin,
                CanCreateTeam = canCreateTeam,
                CanCreatePortfolio = canCreatePortfolio,
                SystemAdminDisplayNames = systemAdminDisplayNames,
                AdminTeamIds = adminTeamIds,
                AdminPortfolioIds = adminPortfolioIds,
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
                    $"{UserProfileString} was not found.");
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
                GrantedAt = timeProvider.GetUtcNow().UtcDateTime,
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

        public async Task<RbacOperationResult> DeleteUserAsync(int userProfileId, CancellationToken cancellationToken = default)
        {
            var userProfile = await context.UserProfiles
                .SingleOrDefaultAsync(p => p.Id == userProfileId, cancellationToken);

            if (userProfile is null)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.UserNotFound,
                    $"{UserProfileString} was not found.");
            }

            var permissions = await context.UserPermissions
                .Where(p => p.UserProfileId == userProfileId)
                .ToListAsync(cancellationToken);

            if (permissions.Count > 0)
            {
                context.UserPermissions.RemoveRange(permissions);
            }

            context.UserProfiles.Remove(userProfile);
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);

            return effectiveAccess.IsSystemAdmin;
        }

        public async Task<bool> IsRestrictedApiKeyAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            if (!TryGetApiKeyId(principal, out var apiKeyId))
            {
                return false;
            }

            return await context.ApiKeyPermissions.AnyAsync(permission => permission.ApiKeyId == apiKeyId, cancellationToken);
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);

            return effectiveAccess.CanWrite(PermissionScopeType.Team, teamId);
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

            var effectiveAccess = await GetEffectiveAccessAsync(principal, currentUser, cancellationToken);

            return effectiveAccess.CanWrite(PermissionScopeType.Portfolio, portfolioId);
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
                    $"{UserProfileString} was not found.");
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
                    GrantedAt = timeProvider.GetUtcNow().UtcDateTime,
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
                    $"{UserProfileString} was not found.");
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
                    GrantedAt = timeProvider.GetUtcNow().UtcDateTime,
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

        public async Task<IReadOnlyList<RbacGroupMappingSummary>> GetGroupMappingsAsync(CancellationToken cancellationToken = default)
        {
            var storedRows = await context.RbacGroupMappings
                .OrderBy(x => x.GroupValue)
                .ThenBy(x => x.ScopeType)
                .ThenBy(x => x.ScopeId)
                .ThenBy(x => x.Role)
                .ThenBy(x => x.Id)
                .Select(x => new RbacGroupMappingSummary
                {
                    Id = x.Id,
                    GroupValue = x.GroupValue,
                    Role = x.Role,
                    ScopeType = x.ScopeType,
                    ScopeId = x.ScopeId,
                })
                .ToListAsync(cancellationToken);

            return storedRows
                .DistinctBy(x => (x.GroupValue, x.Role, x.ScopeType, x.ScopeId))
                .ToList();
        }

        public async Task<IReadOnlyList<RbacGroupMappingSummary>> GetTeamGroupMappingsAsync(int teamId, CancellationToken cancellationToken = default)
        {
            return await context.RbacGroupMappings
                .Where(x => x.ScopeType == PermissionScopeType.Team && x.ScopeId == teamId)
                .OrderBy(x => x.GroupValue)
                .ThenBy(x => x.Role)
                .Select(x => new RbacGroupMappingSummary
                {
                    Id = x.Id,
                    GroupValue = x.GroupValue,
                    Role = x.Role,
                    ScopeType = x.ScopeType,
                    ScopeId = x.ScopeId,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RbacGroupMappingSummary>> GetPortfolioGroupMappingsAsync(int portfolioId, CancellationToken cancellationToken = default)
        {
            return await context.RbacGroupMappings
                .Where(x => x.ScopeType == PermissionScopeType.Portfolio && x.ScopeId == portfolioId)
                .OrderBy(x => x.GroupValue)
                .ThenBy(x => x.Role)
                .Select(x => new RbacGroupMappingSummary
                {
                    Id = x.Id,
                    GroupValue = x.GroupValue,
                    Role = x.Role,
                    ScopeType = x.ScopeType,
                    ScopeId = x.ScopeId,
                })
                .ToListAsync(cancellationToken);
        }

        public Task<bool> TeamExistsAsync(int teamId, CancellationToken cancellationToken = default)
            => context.Teams.AnyAsync(team => team.Id == teamId, cancellationToken);

        public Task<bool> PortfolioExistsAsync(int portfolioId, CancellationToken cancellationToken = default)
            => context.Portfolios.AnyAsync(portfolio => portfolio.Id == portfolioId, cancellationToken);

        public async Task<IReadOnlyList<SystemWideRule>> GetTeamSystemWideRulesAsync(int teamId, CancellationToken cancellationToken = default)
        {
            return await ReadSystemWideRulesReaching(PermissionScopeType.Team, cancellationToken);
        }

        public async Task<IReadOnlyList<SystemWideRule>> GetPortfolioSystemWideRulesAsync(int portfolioId, CancellationToken cancellationToken = default)
        {
            return await ReadSystemWideRulesReaching(PermissionScopeType.Portfolio, cancellationToken);
        }

        private async Task<IReadOnlyList<SystemWideRule>> ReadSystemWideRulesReaching(PermissionScopeType unitType, CancellationToken cancellationToken)
        {
            var storedRows = await context.RbacGroupMappings
                .Where(x => x.ScopeType == PermissionScopeType.AllUnits && x.ScopeId == null)
                .OrderBy(x => x.GroupValue)
                .ThenBy(x => x.Role)
                .ToListAsync(cancellationToken);

            SystemWideRule[] everyoneSignedIn = await TheSwitch.AnyAsync(setting => setting.EveryoneCanView, cancellationToken)
                ? [SystemWideRule.EveryoneSignedIn]
                : [];

            return everyoneSignedIn
                .Concat(storedRows
                    .Select(row => SystemWideRule.TryReading(row, unitType, out var rule) ? rule : null)
                    .OfType<SystemWideRule>())
                .Distinct()
                .ToList();
        }

        public async Task<EveryoneSignedInSetting> GetEveryoneSignedInAsync(CancellationToken cancellationToken = default)
        {
            return await TheSwitch.FirstOrDefaultAsync(cancellationToken) ?? EveryoneSignedInSetting.Off();
        }

        public async Task<RbacOperationResult> SetEveryoneSignedInAsync(
            ClaimsPrincipal caller,
            bool everyoneCanView,
            Guid? concurrencyToken,
            CancellationToken cancellationToken = default)
        {
            if (!await CanSatisfyRequirementAsync(caller, RbacGuardRequirement.SystemAdmin, cancellationToken: cancellationToken))
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.NotSystemAdmin,
                    "Only a System Admin changes whether everyone signed in can view all Teams and Portfolios.");
            }

            if (concurrencyToken is not { } readToken)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.MissingConcurrencyToken,
                    "The setting was sent without the token it was read with. Reload the page and try again.");
            }

            var stored = await TheSwitchToChange.FirstOrDefaultAsync(cancellationToken);

            if (stored is null)
            {
                context.EveryoneSignedInSettings.Add(EveryoneSignedInSetting.StoredAs(everyoneCanView));
            }
            else
            {
                stored.ChangeTo(everyoneCanView);
                context.ApplyConcurrencyTokenForEdit(stored, readToken);
            }

            await context.SaveChangesAsync(cancellationToken);

            return RbacOperationResult.Success();
        }

        // The instance keeps one switch; should a second row ever appear, the oldest is the one read and saved.
        private IQueryable<EveryoneSignedInSetting> TheSwitchToChange => context.EveryoneSignedInSettings
            .OrderBy(setting => setting.Id)
            .Take(1);

        private IQueryable<EveryoneSignedInSetting> TheSwitch => TheSwitchToChange.AsNoTracking();

        // Read synchronously because the unit roles that need it are worked out lazily, on the first unit question.
        private bool EveryoneSignedInCanView()
        {
            return TheSwitch.Any(setting => setting.EveryoneCanView);
        }

        public async Task<RbacOperationResult> CreateGroupMappingAsync(
            string groupValue,
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId,
            CancellationToken cancellationToken = default)
        {
            var normalizedGroupValue = groupValue.Trim();
            if (string.IsNullOrWhiteSpace(normalizedGroupValue))
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.InvalidScopeForRole,
                    "Group value is required.");
            }

            if (!IsValidGroupMappingScope(role, scopeType, scopeId))
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.InvalidScopeForRole,
                    "Role is invalid for the provided scope.");
            }

            if (await GroupAlreadyHasRoleHereAsync(normalizedGroupValue, role, scopeType, scopeId, cancellationToken))
            {
                return GroupAlreadyHasRoleHere();
            }

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = normalizedGroupValue,
                Role = role,
                ScopeType = scopeType,
                ScopeId = scopeId,
            });

            await context.SaveChangesAsync(cancellationToken);

            return RbacOperationResult.Success();
        }

        public async Task<RbacOperationResult> RemoveGroupMappingAsync(int mappingId, CancellationToken cancellationToken = default)
        {
            var mapping = await context.RbacGroupMappings
                .SingleOrDefaultAsync(x => x.Id == mappingId, cancellationToken);

            if (mapping is null)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.GroupMappingNotFound,
                    "Group mapping was not found.");
            }

            // Two equal rows can be stored by two System Admins adding the same rule at once, because a unique index
            // treats a missing unit as distinct. A twin left behind would keep granting the role, so all go together.
            var equalRows = await RowsGivingGroupRole(mapping.GroupValue, mapping.Role, mapping.ScopeType, mapping.ScopeId)
                .ToListAsync(cancellationToken);

            context.RbacGroupMappings.RemoveRange(equalRows);
            await context.SaveChangesAsync(cancellationToken);

            return RbacOperationResult.Success();
        }

        public async Task<RbacOperationResult> UpdateGroupMappingRoleAsync(
            int mappingId,
            UserRole role,
            Guid? concurrencyToken,
            CancellationToken cancellationToken = default)
        {
            var mapping = await context.RbacGroupMappings
                .SingleOrDefaultAsync(x => x.Id == mappingId, cancellationToken);

            if (mapping is null)
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.GroupMappingNotFound,
                    "Group mapping was not found.");
            }

            if (!IsValidGroupMappingScope(role, mapping.ScopeType, mapping.ScopeId))
            {
                return RbacOperationResult.Failure(
                    RbacOperationErrorCodes.InvalidScopeForRole,
                    "Role is invalid for the provided scope.");
            }

            if (role != mapping.Role
                && await GroupAlreadyHasRoleHereAsync(mapping.GroupValue, role, mapping.ScopeType, mapping.ScopeId, cancellationToken))
            {
                return GroupAlreadyHasRoleHere();
            }

            mapping.Role = role;

            if (concurrencyToken.HasValue)
            {
                context.ApplyConcurrencyTokenForEdit(mapping, concurrencyToken.Value);
            }

            await context.SaveChangesAsync(cancellationToken);

            return RbacOperationResult.Success();
        }

        /// <summary>
        /// Computes the effective access of the current principal. When an
        /// <c>api_key_id</c> claim is present (request authenticated via API key), the
        /// owner's access is limited to the per-key <see cref="ApiKeyPermission"/> rows the
        /// owner covers: a row counts only when the owner holds the same scope with a role of
        /// equal-or-broader priority (SystemAdmin reaches every scope; TeamAdmin/PortfolioAdmin
        /// reach their own scope; Viewer covers only Viewer). When the API key has zero
        /// <see cref="ApiKeyPermission"/> rows, the owner's access is returned unchanged
        /// (backwards-compatibility for legacy keys issued before per-key scoping).
        /// </summary>
        private async Task<EffectiveAccess> GetEffectiveAccessAsync(
            ClaimsPrincipal principal,
            UserProfile currentUser,
            CancellationToken cancellationToken)
        {
            var ownerAccess = await GetOwnerEffectiveAccessAsync(principal, currentUser, cancellationToken);

            if (!TryGetApiKeyId(principal, out var apiKeyId))
            {
                return ownerAccess;
            }

            var apiKeyPermissions = await context.ApiKeyPermissions
                .Where(permission => permission.ApiKeyId == apiKeyId)
                .Select(permission => new PermissionRule(permission.ScopeType, permission.ScopeId, permission.Role))
                .ToListAsync(cancellationToken);

            if (apiKeyPermissions.Count == 0)
            {
                return ownerAccess;
            }

            return ownerAccess.LimitedTo(apiKeyPermissions);
        }

        private async Task<EffectiveAccess> GetOwnerEffectiveAccessAsync(
            ClaimsPrincipal principal,
            UserProfile currentUser,
            CancellationToken cancellationToken)
        {
            var explicitPermissions = await context.UserPermissions
                .Where(permission => permission.UserProfileId == currentUser.Id)
                .Select(permission => new PermissionRule(permission.ScopeType, permission.ScopeId, permission.Role))
                .ToListAsync(cancellationToken);

            var groupRows = (await GetVirtualPermissionsAsync(principal, cancellationToken))
                .ToLookup(row => AppliesTo.TryFrom(row, out _));

            return EffectiveAccess.Resolve(
                ExplicitReplacesGroup([.. groupRows[false]], explicitPermissions),
                groupRows[true],
                EveryoneSignedInCanView);
        }

        // A role granted to the person directly replaces whatever their groups give on the same scope,
        // even a higher one. The direct rows take the place of the first group row they replace, so
        // the order in which units are listed stays the order of the group rows.
        private static IEnumerable<PermissionRule> ExplicitReplacesGroup(
            IReadOnlyList<PermissionRule> groupPermissions,
            IReadOnlyList<PermissionRule> explicitPermissions)
        {
            return groupPermissions
                .SelectMany(groupPermission => ExplicitOrGroupOnSameScope(groupPermission, explicitPermissions))
                .Concat(explicitPermissions);
        }

        private static List<PermissionRule> ExplicitOrGroupOnSameScope(
            PermissionRule groupPermission,
            IReadOnlyList<PermissionRule> explicitPermissions)
        {
            var explicitOnSameScope = explicitPermissions.Where(groupPermission.HasSameScopeAs).ToList();
            return explicitOnSameScope.Count > 0 ? explicitOnSameScope : [groupPermission];
        }

        private static bool TryGetApiKeyId(ClaimsPrincipal principal, out int apiKeyId)
        {
            apiKeyId = 0;
            var claimValue = principal.FindFirst("api_key_id")?.Value;
            if (string.IsNullOrWhiteSpace(claimValue))
            {
                return false;
            }

            return int.TryParse(claimValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out apiKeyId);
        }

        // Principals rebuilt from a stored credential or subject carry no live group
        // claims, so the stored snapshot is their only route to an RbacGroupMapping. An ordinary OIDC
        // cookie principal has no auth_method claim and never reaches the fallback.
        private static bool CannotCarryLiveGroupClaims(ClaimsPrincipal principal)
        {
            var authMethod = principal.FindFirst(ApiKeyPrincipalFactory.AuthMethodClaimType)?.Value;
            return authMethod is ApiKeyPrincipalFactory.AuthMethodValue or ApiKeyPrincipalFactory.AuthMethodEmbedValue;
        }

        private async Task<IReadOnlyList<PermissionRule>> GetVirtualPermissionsAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken)
        {
            var groupClaimName = configuration.Value.GroupClaimName;
            if (string.IsNullOrWhiteSpace(groupClaimName))
            {
                return [];
            }

            if (!GroupClaimParser.TryGetGroupValues(principal, groupClaimName, out var groupValues, out var hasUnsupportedFormat))
            {
                return [];
            }

            if (hasUnsupportedFormat)
            {
                logger.LogWarning(
                    "RBAC group claim payload for claim '{ClaimName}' used unsupported format. Falling back to explicit grants only.",
                    groupClaimName);
                return [];
            }

            if (groupValues.Count == 0 && CannotCarryLiveGroupClaims(principal))
            {
                groupValues = await LoadOwnerGroupSnapshotAsync(principal, cancellationToken);
            }

            if (groupValues.Count == 0)
            {
                return [];
            }

            return await context.RbacGroupMappings
                .Where(mapping => groupValues.Contains(mapping.GroupValue))
                .Select(mapping => new PermissionRule(mapping.ScopeType, mapping.ScopeId, mapping.Role))
                .ToListAsync(cancellationToken);
        }

        private async Task<HashSet<string>> LoadOwnerGroupSnapshotAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken)
        {
            var ownerSubject = principal.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(ownerSubject))
            {
                return [];
            }

            var snapshot = await context.UserProfiles
                .Where(p => p.Subject == ownerSubject)
                .Select(p => p.LastKnownGroupClaimValues)
                .SingleOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(snapshot))
            {
                return [];
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(snapshot);
                if (parsed is null)
                {
                    return [];
                }

                return parsed
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .ToHashSet(StringComparer.Ordinal);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to deserialise UserProfile.LastKnownGroupClaimValues snapshot for owner subject {Subject}. Falling back to empty.",
                    ownerSubject);
                return [];
            }
        }

        private static bool IsValidGroupMappingScope(UserRole role, PermissionScopeType scopeType, int? scopeId)
        {
            if (scopeType == PermissionScopeType.AllUnits)
            {
                return AppliesTo.TryFrom(new PermissionRule(scopeType, scopeId, role), out _);
            }

            return role switch
            {
                UserRole.SystemAdmin => scopeType == PermissionScopeType.System && !scopeId.HasValue,
                UserRole.TeamAdmin => scopeType == PermissionScopeType.Team && scopeId.HasValue,
                UserRole.PortfolioAdmin => scopeType == PermissionScopeType.Portfolio && scopeId.HasValue,
                UserRole.Viewer => (scopeType is PermissionScopeType.Team or PermissionScopeType.Portfolio) && scopeId.HasValue,
                _ => false,
            };
        }

        private Task<bool> GroupAlreadyHasRoleHereAsync(
            string groupValue,
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId,
            CancellationToken cancellationToken)
        {
            return RowsGivingGroupRole(groupValue, role, scopeType, scopeId).AnyAsync(cancellationToken);
        }

        private IQueryable<RbacGroupMapping> RowsGivingGroupRole(
            string groupValue,
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId)
        {
            return context.RbacGroupMappings.Where(
                x => x.GroupValue == groupValue
                    && x.Role == role
                    && x.ScopeType == scopeType
                    && x.ScopeId == scopeId);
        }

        private static RbacOperationResult GroupAlreadyHasRoleHere()
        {
            return RbacOperationResult.Failure(
                RbacOperationErrorCodes.DuplicateGroupMapping,
                "This group already has this role here.");
        }

        private Task<bool> HasSystemAdminAsync(CancellationToken cancellationToken)
        {
            return context.UserPermissions.AnyAsync(
                p => p.ScopeType == PermissionScopeType.System && p.Role == UserRole.SystemAdmin,
                cancellationToken);
        }

        private Task<bool> HasAnyTeamAsync(CancellationToken cancellationToken)
        {
            return context.Teams.AnyAsync(cancellationToken);
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

        public async Task<RbacOperationResult> GrantCreatorTeamAdminAsync(
            int userProfileId,
            int teamId,
            CancellationToken cancellationToken = default)
        {
            return await GrantScopedAdminAsync(
                userProfileId,
                PermissionScopeType.Team,
                teamId,
                UserRole.TeamAdmin,
                cancellationToken);
        }

        public async Task<RbacOperationResult> GrantCreatorPortfolioAdminAsync(
            int userProfileId,
            int portfolioId,
            CancellationToken cancellationToken = default)
        {
            return await GrantScopedAdminAsync(
                userProfileId,
                PermissionScopeType.Portfolio,
                portfolioId,
                UserRole.PortfolioAdmin,
                cancellationToken);
        }

        public Task EnsureCreatorTeamAdminAsync(
            ClaimsPrincipal principal,
            int teamId,
            CancellationToken cancellationToken = default)
        {
            return EnsureCreatorAdminAsync(
                principal,
                teamId,
                GrantCreatorTeamAdminAsync,
                cancellationToken);
        }

        public Task EnsureCreatorPortfolioAdminAsync(
            ClaimsPrincipal principal,
            int portfolioId,
            CancellationToken cancellationToken = default)
        {
            return EnsureCreatorAdminAsync(
                principal,
                portfolioId,
                GrantCreatorPortfolioAdminAsync,
                cancellationToken);
        }

        private async Task EnsureCreatorAdminAsync(
            ClaimsPrincipal principal,
            int scopeId,
            Func<int, int, CancellationToken, Task<RbacOperationResult>> grant,
            CancellationToken cancellationToken)
        {
            if (!await IsRbacEnforcedAsync(cancellationToken))
            {
                return;
            }

            var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
            if (currentUser is null)
            {
                return;
            }

            await grant(currentUser.Id, scopeId, cancellationToken);
        }

        private async Task<RbacOperationResult> GrantScopedAdminAsync(
            int userProfileId,
            PermissionScopeType scopeType,
            int scopeId,
            UserRole role,
            CancellationToken cancellationToken)
        {
            var existingPermission = await context.UserPermissions
                .AnyAsync(
                    p => p.UserProfileId == userProfileId
                        && p.ScopeType == scopeType
                        && p.ScopeId == scopeId
                        && p.Role == role,
                    cancellationToken);

            if (existingPermission)
            {
                return RbacOperationResult.Success();
            }

            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = userProfileId,
                ScopeType = scopeType,
                ScopeId = scopeId,
                Role = role,
                GrantedAt = timeProvider.GetUtcNow().UtcDateTime,
            });

            await context.SaveChangesAsync(cancellationToken);
            return RbacOperationResult.Success();
        }
    }
}