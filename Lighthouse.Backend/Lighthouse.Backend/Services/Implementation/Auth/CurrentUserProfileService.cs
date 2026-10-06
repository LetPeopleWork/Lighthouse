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
        public async Task<UserProfile?> GetOrCreateFromPrincipalAsync(
    ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var stableSubject = ResolveStableSubject(principal);
            if (stableSubject is null)
            {
                logger.LogWarning("Current user profile rejected due to missing stable subject claim");
                return null;
            }

            var existingProfile = await context.UserProfiles
                .SingleOrDefaultAsync(p => p.Subject == stableSubject.Value.subject, cancellationToken);

            // An API key carries none of its owner's name or email, only the key's placeholder name, so
            // copying it over would wipe the owner's account details. It speaks for an existing person only.
            if (IsApiKeyCall(principal))
            {
                return existingProfile;
            }

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

            // Bypass the change tracker entirely — only UPDATE this one row
            var displayName = ResolveDisplayName(principal);
            var email = ResolveEmail(principal);
            var now = DateTime.UtcNow;

            await context.UserProfiles
                .Where(p => p.Id == existingProfile.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.DisplayName, displayName)
                    .SetProperty(p => p.Email, email)
                    .SetProperty(p => p.LastSeenAt, now),
                    cancellationToken);

            // Keep the in-memory object consistent with what we just wrote
            existingProfile.DisplayName = displayName;
            existingProfile.Email = email;
            existingProfile.LastSeenAt = now;

            return existingProfile;
        }

        /// <summary>The subject a profile is keyed by, read off the session without touching the store.</summary>
        public static string? StableSubjectOf(ClaimsPrincipal principal) => ResolveStableSubject(principal)?.subject;

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

        private static bool IsApiKeyCall(ClaimsPrincipal principal)
            => principal.HasClaim(ApiKeyPrincipalFactory.AuthMethodClaimType, ApiKeyPrincipalFactory.AuthMethodValue);

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