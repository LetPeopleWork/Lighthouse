using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public sealed class OidcGroupSnapshotWriter(
        LighthouseAppContext context,
        ILogger<OidcGroupSnapshotWriter> logger) : IOidcGroupSnapshotWriter
    {
        public async Task WriteAsync(
            string stableSubject,
            IReadOnlyList<string> groupValues,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(stableSubject);
            ArgumentNullException.ThrowIfNull(groupValues);

            var serialised = JsonSerializer.Serialize(groupValues);

            var existing = await context.UserProfiles
                .SingleOrDefaultAsync(p => p.Subject == stableSubject, cancellationToken);

            if (existing is null)
            {
                logger.LogDebug(
                    "Creating UserProfile during OIDC group-snapshot write for subject {Subject}",
                    stableSubject);

                var created = new UserProfile
                {
                    Subject = stableSubject,
                    SubjectClaimType = "sub",
                    LastKnownGroupClaimValues = serialised,
                };
                context.UserProfiles.Add(created);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            await context.UserProfiles
                .Where(p => p.Id == existing.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.LastKnownGroupClaimValues, serialised),
                    cancellationToken);

            existing.LastKnownGroupClaimValues = serialised;
        }
    }
}
