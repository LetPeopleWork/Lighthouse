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

        // One predicate again, for the reason the two writes above give. There is no condition on
        // what the row answered: a browser only reports this when it was actually shown the dialog,
        // and a repository that second-guessed that would have to re-derive the licence rule that
        // decided it - the same rule, in a second place, free to drift.
        public Task<int> TryMarkAskedAsync(string tokenHash, DateTime askedAt, CancellationToken cancellationToken)
        {
            return context.UsageDataConsents
                .Where(consent => consent.TokenHash == tokenHash)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(consent => consent.AskedAt, askedAt),
                    cancellationToken);
        }

        // Two conditions, not one, and the second is the whole point.
        //
        // A row is removed when the browser stopped visiting long enough ago - that is what lets a
        // genuinely new browser be asked, and it is measured on when it was last seen rather than on
        // what it answered, because deleting refusals sooner than grants would quietly turn "no"
        // into "ask me again next month".
        //
        // But a browser that refused is still owed something: the dialog told it we would come back
        // in a few months. Delete that row before the few months are up and the next visit is met by
        // the question all over again, early, which is the broken promise the copy exists to avoid.
        // So a row that has not yet reached its re-ask date is never pruned, whatever its age.
        //
        // In a normal configuration retention is comfortably longer than the re-ask window and the
        // second condition never binds. It is written down anyway: if it were left implicit, tuning
        // retention below the re-ask window would reintroduce the early ask with nothing failing to
        // say so.
        public Task<int> PruneStaleAsync(
            DateTime lastSeenBefore, DateTime owedNothingSince, CancellationToken cancellationToken)
        {
            return context.UsageDataConsents
                .Where(consent => consent.LastSeenAt < lastSeenBefore
                    && (consent.Decision == UsageDataDecision.Granted
                        || (consent.AskedAt ?? consent.DecidedAt) < owedNothingSince))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
