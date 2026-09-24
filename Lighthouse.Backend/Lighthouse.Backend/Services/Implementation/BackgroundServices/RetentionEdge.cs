using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// The oldest day an owner still keeps finished work from. Every refresh deletes what finished
    /// before it, so a day worked out afterwards may only read a stretch that lies wholly on or after
    /// this edge: a stretch reaching past it is averaged over work that is partly gone, reads lower
    /// than the owner ever was, and is written once and never corrected.
    ///
    /// Judged from the owner's last refresh rather than from the day being worked out, because that
    /// refresh is what deleted the work. A day turned away here stays turned away until the next
    /// refresh, which only moves the edge forward - so the refusal is as settled as any other day the
    /// walk declines, and is remembered the same way.
    ///
    /// Applied to each reading rather than to the day, because how far back a reading looks belongs to
    /// the reading: a thirty-day cycle time window can lie inside what is kept on a day where the
    /// ninety-day one does not. The window the reading is asked for is the one the writers chose, so
    /// this never works out a window of its own.
    /// </summary>
    internal sealed class RetentionEdge
    {
        private const string TheStretchReachesPastTheEdge =
            "The stretch these limits are drawn from reaches back further than finished work is kept.";

        private readonly DateOnly? oldestDayStillKept;

        private readonly DateTime? pinnedStretchStartsOn;

        private RetentionEdge(DateOnly? oldestDayStillKept, DateTime? pinnedStretchStartsOn)
        {
            this.oldestDayStillKept = oldestDayStillKept;
            this.pinnedStretchStartsOn = pinnedStretchStartsOn;
        }

        /// <summary>
        /// A cutoff of zero or less keeps finished work for ever, and then there is no edge. The owner's
        /// pinned stretch is taken along because limits are drawn from it rather than from the day's own
        /// window whenever one is pinned.
        /// </summary>
        public static RetentionEdge For(DateOnly lastObservedOn, WorkTrackingSystemOptionsOwner owner)
            => new(
                owner.DoneItemsCutoffDays > 0 ? lastObservedOn.AddDays(-owner.DoneItemsCutoffDays) : null,
                owner.ProcessBehaviourChartBaselineStartDate);

        /// <summary>
        /// The first day the walk may work out: no day older than the edge has any reading left to
        /// give, whatever its window, so the walk steps over those days rather than spending its
        /// allowance on them.
        /// </summary>
        public DateOnly? NoEarlierThanTheEdge(DateOnly? earliestFinishedDay)
        {
            if (earliestFinishedDay is null || oldestDayStillKept is null)
            {
                return earliestFinishedDay;
            }

            return earliestFinishedDay > oldestDayStillKept ? earliestFinishedDay : oldestDayStillKept;
        }

        /// <summary>A reading whose window reaches past the edge comes back empty, which the writer leaves unwritten.</summary>
        public PercentileFamilyReader Guard(PercentileFamilyReader family)
            => family with
            {
                ReadPercentiles = (windowStart, windowEnd) =>
                    ReachesPastTheEdge(windowStart) ? [] : family.ReadPercentiles(windowStart, windowEnd),
            };

        /// <summary>
        /// Limits whose stretch reaches past the edge come back unusable, which the writer leaves unwritten.
        ///
        /// A pinned stretch is the one the limits come from, so it is the one judged, and the day's own
        /// window says nothing about them. Judging the day being worked out would not catch it: the pinned
        /// stretch does not move with the day, so it lies within the retention of an old enough day long
        /// after the owner's last refresh has deleted it.
        /// </summary>
        public ProcessBehaviorFamilyReader Guard(ProcessBehaviorFamilyReader family)
            => family with
            {
                ReadChart = (windowStart, windowEnd, asOf) =>
                    ReachesPastTheEdge(pinnedStretchStartsOn ?? windowStart)
                        ? ProcessBehaviourChart.NotReady(BaselineStatus.BaselineInvalid, TheStretchReachesPastTheEdge)
                        : family.ReadChart(windowStart, windowEnd, asOf),
            };

        /// <summary>
        /// Both a window start and a pinned stretch start arrive as a calendar day encoded at UTC midnight,
        /// so taking the date is taking the day, not reducing an instant.
        /// </summary>
        private bool ReachesPastTheEdge(DateTime stretchStartsOn)
            => oldestDayStillKept is { } edge && DateOnly.FromDateTime(stretchStartsOn) < edge;
    }
}
