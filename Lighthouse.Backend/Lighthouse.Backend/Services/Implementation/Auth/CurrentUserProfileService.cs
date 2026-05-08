using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class CurrentUserProfileService(
        LighthouseAppContext context,
        ILogger<CurrentUserProfileService> logger) : ICurrentUserProfileService
    {
        public async Task<UserProfile?> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var stableSubject = ResolveStableSubject(principal);
            if (stableSubject is null)
            {
                logger.LogWarning("Current user profile rejected due to missing stable subject claim");
                return null;
            }

            var existingProfile = await context.UserProfiles
                .SingleOrDefaultAsync(p => p.Subject == stableSubject.Value.subject, cancellationToken);

            if (existingProfile is null)
            {
                var createdProfile = new UserProfile
                {
                    Subject = stableSubject.Value.subject,
                    SubjectClaimType = stableSubject.Value.claimType,
                    DisplayName = ResolveDisplayName(principal),
                    Email = ResolveEmail(principal),
                    CreatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow,
                };

                context.UserProfiles.Add(createdProfile);
                await context.SaveChangesAsync(cancellationToken);
                return createdProfile;
            }

            existingProfile.DisplayName = ResolveDisplayName(principal);
            existingProfile.Email = ResolveEmail(principal);
            existingProfile.LastSeenAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            return existingProfile;
        }

        private static (string subject, string claimType)? ResolveStableSubject(ClaimsPrincipal principal)
        {
            var subClaim = principal.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(subClaim))
            {
                return (subClaim, "sub");
            }

            var oidClaim = principal.FindFirst("oid")?.Value;
            if (!string.IsNullOrWhiteSpace(oidClaim))
            {
                return (oidClaim, "oid");
            }

            return null;
        }

        private static string? ResolveDisplayName(ClaimsPrincipal principal)
        {
            return principal.FindFirst("name")?.Value ?? principal.FindFirstValue(ClaimTypes.Name);
        }

        private static string? ResolveEmail(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value;
        }
    }
}