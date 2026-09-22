using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// One process-behaviour family of one owner, together with everything needed to compute it for a
    /// day: which chart to read, and how far back the window that day covers reaches. The span travels
    /// with the family so that a day filled in behind the series cannot pick a different window from
    /// the one the live recording used.
    /// </summary>
    public sealed record ProcessBehaviorFamilyReader(
        ProcessBehaviorMetricType MetricType,
        int LookbackDays,
        Func<DateTime, DateTime, ProcessBehaviourChart> ReadChart);

    /// <summary>
    /// Computes one calendar day of process-behaviour limits for one owner and one family and stages
    /// the row on the snapshot store. Every path that produces a process-behaviour day goes through
    /// here, so a day rebuilt from history and a day recorded live cannot be computed differently.
    ///
    /// Which families a scope has is decided here too. A team has five and a portfolio has six, and
    /// that asymmetry is structural rather than a filter: Feature Size describes a portfolio's items
    /// and there is no team-side chart to read for it. Callers stage whole owners and then save, so
    /// one family failing never discards what another already staged.
    /// </summary>
    public interface IProcessBehaviorSnapshotWriter
    {
        /// <summary>The families a team records, each carrying the window span a team's day covers.</summary>
        IReadOnlyList<ProcessBehaviorFamilyReader> FamiliesFor(Team team);

        /// <summary>The families a portfolio records, each carrying the window span a portfolio's day covers.</summary>
        IReadOnlyList<ProcessBehaviorFamilyReader> FamiliesFor(Portfolio portfolio);

        /// <summary>
        /// Computes today and overwrites whatever is already stored for it. Today's limits legitimately
        /// move as the day goes on, so the last computation of the day is the right one to keep.
        /// </summary>
        void RecordToday(int ownerId, OwnerType ownerType, ProcessBehaviorFamilyReader family);

        /// <summary>
        /// Computes <paramref name="day"/> only where nothing is stored for it yet, and leaves any day
        /// that already carries limits untouched.
        /// </summary>
        void FillDayIfAbsent(int ownerId, OwnerType ownerType, ProcessBehaviorFamilyReader family, DateOnly day);
    }
}
