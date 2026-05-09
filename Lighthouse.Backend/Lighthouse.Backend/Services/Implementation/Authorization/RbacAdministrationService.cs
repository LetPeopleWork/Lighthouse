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

            return new RbacStatus
            {
                Enabled = config.Enabled,
                PremiumGateSatisfied = premiumGateSatisfied,
                HasSystemAdmin = hasSystemAdmin,
                HasEmergencyAdminConfigured = config.EmergencySystemAdminSubjects.Count > 0,
                ReadyForEnablement = premiumGateSatisfied && hasSystemAdmin,
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

            var users = await context.UserProfiles
                .OrderBy(p => p.DisplayName)
                .ThenBy(p => p.Email)
                .Select(p => new RbacUserSummary
                {
                    Id = p.Id,
                    Subject = p.Subject,
                    DisplayName = p.DisplayName,
                    Email = p.Email,
                    IsSystemAdmin = systemAdminIds.Contains(p.Id),
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
                };
            }

            var isSystemAdmin = await CanManageRbacAsync(principal, cancellationToken);
            var canCreateTeam = isSystemAdmin || await CanCreateTeamAsync(principal, cancellationToken);
            var canCreatePortfolio = isSystemAdmin || await CanCreatePortfolioAsync(principal, cancellationToken);

            return new UserAuthorizationSummary
            {
                IsRbacEnabled = true,
                IsSystemAdmin = isSystemAdmin,
                CanCreateTeam = canCreateTeam,
                CanCreatePortfolio = canCreatePortfolio,
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

        private Task<bool> HasSystemAdminAsync(CancellationToken cancellationToken)
        {
            return context.UserPermissions.AnyAsync(
                p => p.ScopeType == PermissionScopeType.System && p.Role == UserRole.SystemAdmin,
                cancellationToken);
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