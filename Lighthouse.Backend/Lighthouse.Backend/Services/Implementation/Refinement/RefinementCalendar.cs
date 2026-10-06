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
            var firstSearch = RefinementCadenceCalendar.DaysSearched(cadence, today);
            var blackoutDays = BlackoutDaysLookedAt(today, firstSearch);

            bool IsBlackedOut(DateOnly day) => blackoutDays.IsBlackoutDay(day);

            var next = RefinementCadenceCalendar.NextAfter(cadence, today, IsBlackedOut);
            var isRefinementDay = RefinementCadenceCalendar.IsCadenceDay(cadence, today, IsBlackedOut);
            blackoutDays.AddRange(BlackoutDaysTheCycleEndSearchAdds(cadence, isRefinementDay ? today : next, firstSearch));

            return new RefinementCalendarFacts(next, isRefinementDay, next?.DayNumber - today.DayNumber)
            {
                Cycle = RefinementCadenceCalendar.CycleFrom(cadence, today, IsBlackedOut),
            };
        }

        // The end of the cycle is searched for from its start, so that search runs a few days past the first one.
        // Only those few days are fetched, never a second year.
        private List<BlackoutPeriod> BlackoutDaysTheCycleEndSearchAdds(
            RefinementCadence cadence, DateOnly? cycleStart, (DateOnly First, DateOnly Last)? firstSearch)
        {
            if (cycleStart is not { } start
                || firstSearch is not { } first
                || RefinementCadenceCalendar.DaysSearched(cadence, start) is not { } second
                || second.Last <= first.Last)
            {
                return [];
            }

            return BlackoutDaysBetween(first.Last.AddDays(1), second.Last);
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
