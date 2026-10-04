using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>
    /// Which days a Team's Refinement cadence falls on. The next Refinement is strictly after today: on a
    /// Refinement day the tab already looks to the following one. A blackout day never moves a Refinement.
    /// </summary>
    public static class RefinementCadenceCalendar
    {
        public static DateOnly? NextAfter(RefinementCadence? cadence, DateOnly today)
        {
            if (cadence is not { Weekdays.Count: > 0, IntervalWeeks: > 0 })
            {
                return null;
            }

            var anchorWeek = AnchorWeekOf(cadence, today);
            var tomorrow = today.AddDays(1);
            var searchFrom = anchorWeek > tomorrow ? anchorWeek : tomorrow;

            return Enumerable.Range(0, 7 * cadence.IntervalWeeks)
                .Select(searchFrom.AddDays)
                .Where(day => WeeklyRecurrence.Matches(cadence.Weekdays, cadence.IntervalWeeks, anchorWeek, day))
                .Select(day => (DateOnly?)day)
                .FirstOrDefault();
        }

        public static bool IsCadenceDay(RefinementCadence? cadence, DateOnly day)
        {
            return cadence is { Weekdays.Count: > 0, IntervalWeeks: > 0 }
                && WeeklyRecurrence.Matches(cadence.Weekdays, cadence.IntervalWeeks, AnchorWeekOf(cadence, day), day);
        }

        // Only a cadence that skips weeks needs a starting week; every week counts the same from any of them.
        private static DateOnly AnchorWeekOf(RefinementCadence cadence, DateOnly today)
            => WeeklyRecurrence.MondayOfWeek(cadence.AnchorWeek ?? today);
    }
}
