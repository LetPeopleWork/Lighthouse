using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Computes one calendar day of percentile snapshot rows for one owner and one metric family and
    /// stages them on the snapshot store. Every path that produces a percentile day goes through here,
    /// so a day rebuilt from history and a day recorded live cannot be computed differently.
    ///
    /// Recording today stages only: the caller calls Save, so several families staged by one caller
    /// persist together and one failing family never discards what another already staged. Filling a
    /// gap persists through SaveFilledDay instead, because a day that collides with another copy of
    /// the application has to be dropped on its own rather than take the rest of the walk with it.
    ///
    /// The window reader is handed in because the owner kind decides which metrics service answers it.
    /// It is called with the window start and the window end, and returns that window's readings.
    /// </summary>
    public interface IPercentileSnapshotWriter
    {
        /// <summary>
        /// Computes today and overwrites whatever is already stored for it. Today's value legitimately
        /// changes as the day goes on, so the last computation of the day is the right one to keep.
        /// </summary>
        void RecordToday(
            int ownerId,
            OwnerType ownerType,
            MetricType metricType,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles);

        /// <summary>
        /// Computes <paramref name="day"/> only where nothing is stored for it yet, and leaves any day
        /// that already carries a value untouched.
        /// </summary>
        void FillDayIfAbsent(
            int ownerId,
            OwnerType ownerType,
            MetricType metricType,
            DateOnly day,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles);

        /// <summary>
        /// Persists what the last <see cref="FillDayIfAbsent"/> staged, one day at a time.
        ///
        /// Two copies of the application can both find a day absent and both go on to write it. The
        /// database refuses the second on the natural key, and absorbing that refusal here is what
        /// makes the duplicate work cost nothing: the refused row is dropped and the caller carries on
        /// with the next day. Saving per day rather than per pass is the whole point - a collision on
        /// one day must not abandon the eighty-nine after it.
        /// </summary>
        Task SaveFilledDay();
    }
}
