namespace Lighthouse.Backend.Models.Staleness
{
    /// <summary>
    /// What the policy needs to know about one Work Item or Feature, already resolved for the day being
    /// asked about: a past read hands over the state the item held then, not the one it holds today.
    /// </summary>
    public sealed record StalenessCandidate(
        StateCategories StateCategory,
        string StateName,
        DateTime? EnteredStateAt,
        bool IsBlocked,
        DateTime? BlockedSince);

    public sealed class StalenessPolicy
    {
        private readonly int timeInStateThresholdDays;
        private readonly int blockedThresholdDays;

        private StalenessPolicy(int timeInStateThresholdDays, int blockedThresholdDays)
        {
            this.timeInStateThresholdDays = timeInStateThresholdDays;
            this.blockedThresholdDays = blockedThresholdDays;
        }

        public static StalenessPolicy For(WorkTrackingSystemOptionsOwner owner)
            => new(owner.StalenessThresholdDays, owner.BlockedStalenessThresholdDays);

        public StalenessVerdict Evaluate(StalenessCandidate candidate, TimeZoneInfo zone, DateOnly asOfDay)
        {
            if (candidate.StateCategory != StateCategories.Doing)
            {
                return StalenessVerdict.NotStale;
            }

            var daysInState = DaysInStateIfPastThreshold(candidate, zone, asOfDay);

            return candidate.IsBlocked
                ? EvaluateBlocked(candidate, DaysBlockedIfAtThreshold(candidate, zone, asOfDay), daysInState)
                : EvaluateUnblocked(candidate, daysInState);
        }

        /// <summary>
        /// The threshold a stale item crossed. The reason that made it stale is always listed first.
        /// </summary>
        public int ThresholdCrossedBy(StalenessVerdict staleVerdict)
        {
            if (!staleVerdict.IsStale)
            {
                throw new ArgumentException("An item that is not stale has crossed no threshold.", nameof(staleVerdict));
            }

            return staleVerdict.Reasons[0].Kind == StaleReasonKind.BlockedDuration
                ? blockedThresholdDays
                : timeInStateThresholdDays;
        }

        private int? DaysInStateIfPastThreshold(StalenessCandidate candidate, TimeZoneInfo zone, DateOnly asOfDay)
        {
            var daysInState = DaysSince(candidate.EnteredStateAt, zone, asOfDay);
            return timeInStateThresholdDays > 0 && daysInState > timeInStateThresholdDays ? daysInState : null;
        }

        private int? DaysBlockedIfAtThreshold(StalenessCandidate candidate, TimeZoneInfo zone, DateOnly asOfDay)
        {
            var daysBlocked = DaysSince(candidate.BlockedSince, zone, asOfDay);
            return blockedThresholdDays > 0 && daysBlocked >= blockedThresholdDays ? daysBlocked : null;
        }

        private static StalenessVerdict EvaluateUnblocked(StalenessCandidate candidate, int? daysInState)
        {
            return daysInState is int days
                ? new StalenessVerdict(true, [new StaleReason(StaleReasonKind.TimeInState, days, candidate.StateName)])
                : StalenessVerdict.NotStale;
        }

        // A Blocked item is judged by how long it has been Blocked alone. Its time in state is still
        // named beside that, so the reader sees both clocks, but it never makes the item stale itself.
        private static StalenessVerdict EvaluateBlocked(StalenessCandidate candidate, int? daysBlocked, int? daysInState)
        {
            List<StaleReason> reasons = [];

            if (daysBlocked is int blocked)
            {
                reasons.Add(new StaleReason(StaleReasonKind.BlockedDuration, blocked, null));
            }

            if (daysInState is int days)
            {
                reasons.Add(new StaleReason(StaleReasonKind.TimeInStateWhileBlocked, days, candidate.StateName));
            }

            return new StalenessVerdict(daysBlocked.HasValue, reasons);
        }

        // Counted like Work Item Age: the starting day and the day asked about both count, on the
        // instance's calendar, and a start on or after the day asked about still counts as one day.
        private static int? DaysSince(DateTime? instant, TimeZoneInfo zone, DateOnly asOfDay)
        {
            if (instant is not DateTime start)
            {
                return null;
            }

            var startDay = InstanceCalendar.DayOf(start, zone);
            return Math.Max(1, asOfDay.DayNumber - startDay.DayNumber + 1);
        }
    }
}
