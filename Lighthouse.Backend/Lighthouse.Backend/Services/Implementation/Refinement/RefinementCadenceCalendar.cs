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
            if (!HasCadence(cadence) || DaysSearched(cadence, today) is not { } daysSearched)
            {
                return null;
            }

            var anchorWeek = AnchorWeekOf(cadence, today);

            return Enumerable.Range(0, SearchHorizonDays)
                .Select(daysSearched.First.AddDays)
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

        /// <summary>
        /// The days <see cref="NextAfter"/> looks at, so a caller knows which blackout days to fetch. Null when it looks
        /// at none: without a cadence, or when a starting week so late would run the search past the calendar's last day.
        /// </summary>
        public static (DateOnly First, DateOnly Last)? DaysSearched(RefinementCadence? cadence, DateOnly today)
        {
            if (!HasCadence(cadence))
            {
                return null;
            }

            var firstDay = FirstDaySearched(cadence, today);
            if (DateOnly.MaxValue.DayNumber - firstDay.DayNumber < SearchHorizonDays - 1)
            {
                return null;
            }

            return (firstDay, firstDay.AddDays(SearchHorizonDays - 1));
        }

        private static bool HasCadence([NotNullWhen(true)] RefinementCadence? cadence)
            => cadence is { Weekdays.Count: > 0, IntervalWeeks: > 0 };

        private static DateOnly FirstDaySearched(RefinementCadence cadence, DateOnly today)
        {
            var anchorWeek = AnchorWeekOf(cadence, today);
            var tomorrow = today.AddDays(1);
            return anchorWeek > tomorrow ? anchorWeek : tomorrow;
        }

        // A cadence without a starting week refines every week, where any week counts the same, so today's week will do.
        // A starting week is honoured whenever there is one: no Refinement falls before it.
        private static DateOnly AnchorWeekOf(RefinementCadence cadence, DateOnly today)
            => WeeklyRecurrence.MondayOfWeek(cadence.AnchorWeek ?? today);
    }
}
