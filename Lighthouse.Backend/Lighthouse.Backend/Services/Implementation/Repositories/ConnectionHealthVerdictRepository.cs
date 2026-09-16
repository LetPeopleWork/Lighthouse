using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.ConnectionHealth;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class ConnectionHealthVerdictRepository(LighthouseAppContext context, ILogger<ConnectionHealthVerdictRepository> logger)
        : RepositoryBase<ConnectionHealthVerdict>(context, ctx => ctx.ConnectionHealthVerdicts, logger)
    {
        /// <summary>
        /// Wins, or does not win, the right to ask this connection how it is. The row itself is the claim:
        /// the update only matches while the recorded moment is still older than the cutoff, so of several
        /// instances reaching the same stale connection on the same tick exactly one sees a row change and
        /// the rest find nothing to do. A connection with no row at all is claimed by inserting one, and
        /// the unique index on the connection id settles that race the same way.
        ///
        /// The moment is advanced before the question is asked rather than after. A claim taken once the
        /// answer is back has already let every other instance start asking, which is the whole thing it
        /// exists to prevent; the cost is that the moment describes when the asking began, and the answer
        /// overwrites it a request later.
        /// </summary>
        public async Task<bool> TryClaimForProbeAsync(int connectionId, DateTime cutoff, DateTime now, CancellationToken cancellationToken)
        {
            var claimed = await Context.ConnectionHealthVerdicts
                .Where(verdict => verdict.WorkTrackingSystemConnectionId == connectionId && verdict.ObservedAt < cutoff)
                .ExecuteUpdateAsync(setters => setters.SetProperty(verdict => verdict.ObservedAt, now), cancellationToken);

            if (claimed > 0)
            {
                // That update went straight to the database, so a copy of this row read earlier in the same
                // scope still holds the moment it had before the claim. Whoever reads it next would get the
                // old one back and could write it out again, handing the connection back to the next pass as
                // though nothing had claimed it - a connection asked on every pass forever.
                ForgetAnyTrackedCopyOf(connectionId);
                return true;
            }

            return !await Context.ConnectionHealthVerdicts
                       .AnyAsync(verdict => verdict.WorkTrackingSystemConnectionId == connectionId, cancellationToken)
                   && await TryInsertClaimAsync(connectionId, now);
        }

        private async Task<bool> TryInsertClaimAsync(int connectionId, DateTime now)
        {
            Add(new ConnectionHealthVerdict
            {
                WorkTrackingSystemConnectionId = connectionId,
                State = ConnectionHealthState.Unknown,
                Code = string.Empty,
                Message = string.Empty,
                ObservedAt = now,
            });

            try
            {
                await Save();
                return true;
            }
            catch (DbUpdateException)
            {
                // Another instance inserted the first verdict for this connection between the check above
                // and this write, and the unique index refused the second. Losing that race is the
                // mechanism working, not a fault - the connection is being asked, just not by us.
                //
                // Only the row that was refused is let go. Emptying the whole change tracker would also
                // throw away whatever else the caller had in flight on this context, and this repository
                // is shared with the request that is using it.
                ForgetAnyTrackedCopyOf(connectionId);
                return false;
            }
        }

        private void ForgetAnyTrackedCopyOf(int connectionId)
        {
            var tracked = Context.ChangeTracker.Entries<ConnectionHealthVerdict>()
                .Where(entry => entry.Entity.WorkTrackingSystemConnectionId == connectionId)
                .ToList();

            foreach (var entry in tracked)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
