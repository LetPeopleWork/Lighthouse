using System.Diagnostics.CodeAnalysis;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>
    /// Which days a Team's Refinement cadence falls on. The next Refinement is strictly after today: on a
    /// Refinement day the tab already looks to the following one. A Refinement on a blackout day is skipped.
    /// </summary>
    public static class RefinementCadenceCalendar
    {
        // A year of blacked-out Refinement days in a row means no Refinement worth naming, and stops the search.
        private const int SearchHorizonDays = 366;

        public static DateOnly? NextAfter(RefinementCadence? cadence, DateOnly today, Func<DateOnly, bool> isBlackedOut)
        {
            if (!HasCadence(cadence))
            {
                return null;
            }

            var anchorWeek = AnchorWeekOf(cadence, today);

            return Enumerable.Range(0, SearchHorizonDays)
                .Select(FirstDaySearched(cadence, today).AddDays)
                .Where(day => WeeklyRecurrence.Matches(cadence.Weekdays, cadence.IntervalWeeks, anchorWeek, day))
                .Where(day => !isBlackedOut(day))
                .Select(day => (DateOnly?)day)
                .FirstOrDefault();
        }

        public static bool IsCadenceDay(RefinementCadence? cadence, DateOnly day, Func<DateOnly, bool> isBlackedOut)
        {
            return HasCadence(cadence)
                && WeeklyRecurrence.Matches(cadence.Weekdays, cadence.IntervalWeeks, AnchorWeekOf(cadence, day), day)
                && !isBlackedOut(day);
        }

        /// <summary>The last day <see cref="NextAfter"/> looks at, so a caller knows which blackout days to fetch.</summary>
        public static DateOnly LastDaySearched(RefinementCadence? cadence, DateOnly today)
        {
            var firstDay = HasCadence(cadence) ? FirstDaySearched(cadence, today) : today.AddDays(1);
            return firstDay.AddDays(SearchHorizonDays - 1);
        }

        private static bool HasCadence([NotNullWhen(true)] RefinementCadence? cadence)
            => cadence is { Weekdays.Count: > 0, IntervalWeeks: > 0 };

        private static DateOnly FirstDaySearched(RefinementCadence cadence, DateOnly today)
        {
            var anchorWeek = AnchorWeekOf(cadence, today);
            var tomorrow = today.AddDays(1);
            return anchorWeek > tomorrow ? anchorWeek : tomorrow;
        }

        // Only a cadence that skips weeks needs a starting week; every week counts the same from any of them.
        private static DateOnly AnchorWeekOf(RefinementCadence cadence, DateOnly today)
            => WeeklyRecurrence.MondayOfWeek(cadence.AnchorWeek ?? today);
    }
}
