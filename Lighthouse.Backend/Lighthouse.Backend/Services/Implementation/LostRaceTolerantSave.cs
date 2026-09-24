using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Commits one filled-in day where another copy of the application may have written the same day
    /// first. The database refuses the second copy on the natural key, and when every refused row is
    /// one that is now stored, the day is present either way - which is all that was wanted - so the
    /// refusal is absorbed. Any other refusal is thrown on.
    ///
    /// Both over-time writers commit through here so the two cannot come to disagree about which
    /// refusals are harmless.
    /// </summary>
    internal static class LostRaceTolerantSave
    {
        public static async Task SaveAsync(Func<Task> save, Func<object, bool> isAlreadyStored)
        {
            // Every iteration that absorbs a refusal drops at least one staged row, and there are
            // finitely many, so the loop runs at most once per row this day carries.
            while (true)
            {
                try
                {
                    await save();
                    return;
                }
                catch (DbUpdateException refused)
                {
                    var somebodyElseGotThereFirst = refused.Entries.Count > 0 && refused.Entries.All(entry =>
                        entry.State == EntityState.Added && isAlreadyStored(entry.Entity));

                    // Dropped either way. A refused row left staged would be retried on the next
                    // day's save and fail again, so one bad day would take the rest of the walk.
                    DropFromTheStagingArea(refused);

                    if (!somebodyElseGotThereFirst)
                    {
                        throw;
                    }
                }
            }
        }

        private static void DropFromTheStagingArea(DbUpdateException refused)
        {
            foreach (var entry in refused.Entries)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
