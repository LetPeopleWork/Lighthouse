using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class UsageDataConsentRepository(LighthouseAppContext context) : IUsageDataConsentRepository
    {
        public async Task AddAsync(UsageDataConsent consent, CancellationToken cancellationToken)
        {
            await context.UsageDataConsents.AddAsync(consent, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        public Task<UsageDataConsent?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return context.UsageDataConsents
                .AsNoTracking()
                .SingleOrDefaultAsync(consent => consent.TokenHash == tokenHash, cancellationToken);
        }

        // Every condition that decides whether this write may happen is inside the one predicate. A
        // check performed first and a write performed second is the same thing with a gap in the
        // middle, and the gap is where a concurrent withdrawal gets overwritten.
        public Task<int> TouchAsync(string tokenHash, DateTime seenAt, DateTime staleBefore, CancellationToken cancellationToken)
        {
            return context.UsageDataConsents
                .Where(consent => consent.TokenHash == tokenHash
                    && consent.LastSeenAt < staleBefore)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(consent => consent.LastSeenAt, seenAt),
                    cancellationToken);
        }

        // Only a grant can be withdrawn. Withdrawing a refusal would be meaningless, and withdrawing
        // an already-withdrawn row would move its timestamp for no reason, making the record of when
        // somebody actually changed their mind untrue.
        public Task<int> TryRevokeAsync(string tokenHash, DateTime revokedAt, CancellationToken cancellationToken)
        {
            return context.UsageDataConsents
                .Where(consent => consent.TokenHash == tokenHash
                    && consent.Decision == UsageDataDecision.Granted)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(consent => consent.Decision, UsageDataDecision.Revoked)
                        .SetProperty(consent => consent.DecidedAt, revokedAt),
                    cancellationToken);
        }

        public Task<bool> AnyLiveGrantAsync(DateTime livenessThreshold, CancellationToken cancellationToken)
        {
            return context.UsageDataConsents
                .AnyAsync(consent => consent.Decision == UsageDataDecision.Granted
                    && consent.LastSeenAt > livenessThreshold,
                    cancellationToken);
        }

        // Rows are removed on how long ago the browser was last seen, never on what it answered. A
        // refusal that ages out is a browser that has stopped visiting, and deleting it is what lets
        // a genuinely new browser be asked; deleting refusals sooner than grants would quietly turn
        // "no" into "ask me again next month".
        public Task<int> PruneStaleAsync(DateTime threshold, CancellationToken cancellationToken)
        {
            return context.UsageDataConsents
                .Where(consent => consent.LastSeenAt < threshold)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
