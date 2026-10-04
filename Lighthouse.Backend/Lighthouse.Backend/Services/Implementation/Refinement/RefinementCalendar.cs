using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public sealed class RefinementCalendar(ILighthouseClock clock, IBlackoutPeriodService blackoutPeriodService) : IRefinementCalendar
    {
        public RefinementCalendarFacts FactsFor(RefinementCadence? cadence)
        {
            if (cadence is null)
            {
                return RefinementCalendarFacts.None;
            }

            var today = clock.Today;
            var blackoutDays = BlackoutDaysLookedAt(today, RefinementCadenceCalendar.DaysSearched(cadence, today));

            bool IsBlackedOut(DateOnly day) => blackoutDays.IsBlackoutDay(day);

            var next = RefinementCadenceCalendar.NextAfter(cadence, today, IsBlackedOut);

            return new RefinementCalendarFacts(
                next,
                RefinementCadenceCalendar.IsCadenceDay(cadence, today, IsBlackedOut),
                next?.DayNumber - today.DayNumber);
        }

        // A starting week far ahead puts the search years away from today. Fetching everything in between would
        // work out every recurring blackout across all those years on each read of the tab, so only today and the
        // days searched are fetched.
        private List<BlackoutPeriod> BlackoutDaysLookedAt(DateOnly today, (DateOnly First, DateOnly Last)? searched)
        {
            if (searched is not { } days)
            {
                return BlackoutDaysBetween(today, today);
            }

            if (days.First == today.AddDays(1))
            {
                return BlackoutDaysBetween(today, days.Last);
            }

            return [.. BlackoutDaysBetween(today, today), .. BlackoutDaysBetween(days.First, days.Last)];
        }

        private List<BlackoutPeriod> BlackoutDaysBetween(DateOnly first, DateOnly last)
            => [.. blackoutPeriodService.GetEffectiveBlackoutDays(first.ToDateTime(TimeOnly.MinValue), last.ToDateTime(TimeOnly.MinValue))];
    }
}
