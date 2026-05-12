using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    public class RbacAdministrationService(
        LighthouseAppContext context,
        IOptions<AuthorizationConfiguration> configuration,
        ILicenseService licenseService,
        ICurrentUserProfileService currentUserProfileService,
        ILogger<RbacAdministrationService> logger) : IRbacAdministrationService
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);

            return distinctTeamIds
                .Where(teamId => HasTeamReadPermission(effectivePermissions, teamId))
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);

            return distinctPortfolioIds
                .Where(portfolioId => HasPortfolioReadPermission(effectivePermissions, portfolioId))
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);
            return HasTeamReadPermission(effectivePermissions, teamId);
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);
            return HasTeamWritePermission(effectivePermissions, teamId);
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);
            return HasPortfolioReadPermission(effectivePermissions, portfolioId);
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);
            return HasPortfolioWritePermission(effectivePermissions, portfolioId);
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

            var adminTeamIds = new List<int>();
            var adminPortfolioIds = new List<int>();

            if (!isSystemAdmin)
            {
                var currentUser = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);
                if (currentUser is not null)
                {
                    var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);
                    adminTeamIds.AddRange(
                        effectivePermissions
                            .Where(e => e.Key.ScopeType == PermissionScopeType.Team && e.Key.ScopeId.HasValue && e.Value == UserRole.TeamAdmin)
                            .Select(e => e.Key.ScopeId!.Value));
                    adminPortfolioIds.AddRange(
                        effectivePermissions
                            .Where(e => e.Key.ScopeType == PermissionScopeType.Portfolio && e.Key.ScopeId.HasValue && e.Value == UserRole.PortfolioAdmin)
                            .Select(e => e.Key.ScopeId!.Value));
                }
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);

            return effectivePermissions.TryGetValue(
                       new PermissionScopeKey(PermissionScopeType.System, null),
                       out var role)
                   && role == UserRole.SystemAdmin;
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);

            return HasTeamWritePermission(effectivePermissions, teamId);
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

            var effectivePermissions = await GetEffectivePermissionsAsync(principal, currentUser, cancellationToken);

            return HasPortfolioWritePermission(effectivePermissions, portfolioId);
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

        public async Task<IReadOnlyList<RbacGroupMappingSummary>> GetGroupMappingsAsync(CancellationToken cancellationToken = default)
        {
            return await context.RbacGroupMappings
                .OrderBy(x => x.GroupValue)
                .ThenBy(x => x.ScopeType)
                .ThenBy(x => x.ScopeId)
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

            var alreadyExists = await context.RbacGroupMappings.AnyAsync(
                x => x.GroupValue == normalizedGroupValue
                    && x.Role == role
                    && x.ScopeType == scopeType
                    && x.ScopeId == scopeId,
                cancellationToken);

            if (alreadyExists)
            {
                return RbacOperationResult.Success();
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

            context.RbacGroupMappings.Remove(mapping);
            await context.SaveChangesAsync(cancellationToken);

            return RbacOperationResult.Success();
        }

        private async Task<Dictionary<PermissionScopeKey, UserRole>> GetEffectivePermissionsAsync(
            ClaimsPrincipal principal,
            UserProfile currentUser,
            CancellationToken cancellationToken)
        {
            var explicitPermissions = await context.UserPermissions
                .Where(permission => permission.UserProfileId == currentUser.Id)
                .Select(permission => new PermissionRule(permission.ScopeType, permission.ScopeId, permission.Role))
                .ToListAsync(cancellationToken);

            var virtualPermissions = await GetVirtualPermissionsAsync(principal, cancellationToken);

            var virtualPermissionMap = ToHighestRoleMap(virtualPermissions);
            var explicitPermissionMap = ToHighestRoleMap(explicitPermissions);

            foreach (var explicitEntry in explicitPermissionMap)
            {
                virtualPermissionMap[explicitEntry.Key] = explicitEntry.Value;
            }

            return virtualPermissionMap;
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

            if (!TryGetGroupValues(principal, groupClaimName, out var groupValues, out var hasUnsupportedFormat))
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

            if (groupValues.Count == 0)
            {
                return [];
            }

            return await context.RbacGroupMappings
                .Where(mapping => groupValues.Contains(mapping.GroupValue))
                .Select(mapping => new PermissionRule(mapping.ScopeType, mapping.ScopeId, mapping.Role))
                .ToListAsync(cancellationToken);
        }

        private static bool TryGetGroupValues(
            ClaimsPrincipal principal,
            string claimName,
            out HashSet<string> groupValues,
            out bool hasUnsupportedFormat)
        {
            groupValues = [];
            hasUnsupportedFormat = false;

            var claims = principal.FindAll(claimName);
            foreach (var claim in claims)
            {
                var claimValue = claim.Value.Trim();
                if (string.IsNullOrWhiteSpace(claimValue))
                {
                    continue;
                }

                if (claimValue.StartsWith('['))
                {
                    if (!TryParseJsonArrayClaim(claimValue, out var parsedValues))
                    {
                        hasUnsupportedFormat = true;
                        return true;
                    }

                    foreach (var parsedValue in parsedValues)
                    {
                        groupValues.Add(parsedValue);
                    }

                    continue;
                }

                if (claimValue.StartsWith('{'))
                {
                    hasUnsupportedFormat = true;
                    return true;
                }

                groupValues.Add(claimValue);
            }

            return true;
        }

        private static bool TryParseJsonArrayClaim(string claimValue, out IReadOnlyList<string> values)
        {
            values = [];

            try
            {
                using var document = JsonDocument.Parse(claimValue);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var parsedValues = new List<string>();
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String)
                    {
                        return false;
                    }

                    var value = element.GetString();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    parsedValues.Add(value.Trim());
                }

                values = parsedValues;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static Dictionary<PermissionScopeKey, UserRole> ToHighestRoleMap(IEnumerable<PermissionRule> permissions)
        {
            var result = new Dictionary<PermissionScopeKey, UserRole>();

            foreach (var permission in permissions)
            {
                var key = new PermissionScopeKey(permission.ScopeType, permission.ScopeId);
                if (!result.TryGetValue(key, out var existingRole)
                    || GetRolePriority(permission.Role) > GetRolePriority(existingRole))
                {
                    result[key] = permission.Role;
                }
            }

            return result;
        }

        private static int GetRolePriority(UserRole role)
        {
            return role switch
            {
                UserRole.SystemAdmin => 4,
                UserRole.TeamAdmin => 3,
                UserRole.PortfolioAdmin => 3,
                UserRole.Viewer => 2,
                _ => 0,
            };
        }

        private static bool HasTeamReadPermission(
            IReadOnlyDictionary<PermissionScopeKey, UserRole> effectivePermissions,
            int teamId)
        {
            return effectivePermissions.TryGetValue(new PermissionScopeKey(PermissionScopeType.Team, teamId), out var role)
                && (role == UserRole.TeamAdmin || role == UserRole.Viewer);
        }

        private static bool HasTeamWritePermission(
            IReadOnlyDictionary<PermissionScopeKey, UserRole> effectivePermissions,
            int teamId)
        {
            return effectivePermissions.TryGetValue(new PermissionScopeKey(PermissionScopeType.Team, teamId), out var role)
                && role == UserRole.TeamAdmin;
        }

        private static bool HasPortfolioReadPermission(
            IReadOnlyDictionary<PermissionScopeKey, UserRole> effectivePermissions,
            int portfolioId)
        {
            return effectivePermissions.TryGetValue(new PermissionScopeKey(PermissionScopeType.Portfolio, portfolioId), out var role)
                && (role == UserRole.PortfolioAdmin || role == UserRole.Viewer);
        }

        private static bool HasPortfolioWritePermission(
            IReadOnlyDictionary<PermissionScopeKey, UserRole> effectivePermissions,
            int portfolioId)
        {
            return effectivePermissions.TryGetValue(new PermissionScopeKey(PermissionScopeType.Portfolio, portfolioId), out var role)
                && role == UserRole.PortfolioAdmin;
        }

        private static bool IsValidGroupMappingScope(UserRole role, PermissionScopeType scopeType, int? scopeId)
        {
            return role switch
            {
                UserRole.SystemAdmin => scopeType == PermissionScopeType.System && !scopeId.HasValue,
                UserRole.TeamAdmin => scopeType == PermissionScopeType.Team && scopeId.HasValue,
                UserRole.PortfolioAdmin => scopeType == PermissionScopeType.Portfolio && scopeId.HasValue,
                UserRole.Viewer => (scopeType == PermissionScopeType.Team || scopeType == PermissionScopeType.Portfolio)
                    && scopeId.HasValue,
                _ => false,
            };
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

        private readonly record struct PermissionRule(PermissionScopeType ScopeType, int? ScopeId, UserRole Role);

        private readonly record struct PermissionScopeKey(PermissionScopeType ScopeType, int? ScopeId);

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
                GrantedAt = DateTime.UtcNow,
            });

            await context.SaveChangesAsync(cancellationToken);
            return RbacOperationResult.Success();
        }
    }
}