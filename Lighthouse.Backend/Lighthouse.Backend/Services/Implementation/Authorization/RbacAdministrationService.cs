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
    }
}