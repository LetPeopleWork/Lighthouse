using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public sealed class RefinementCalendar(ILighthouseClock clock, IBlackoutPeriodService blackoutPeriodService) : IRefinementCalendar
    {
        public RefinementCalendarFacts FactsFor(RefinementCadence? cadence)
        {
            var today = clock.Today;
            var blackoutDays = blackoutPeriodService.GetEffectiveBlackoutDays(
                today.ToDateTime(TimeOnly.MinValue),
                RefinementCadenceCalendar.LastDaySearched(cadence, today).ToDateTime(TimeOnly.MinValue));

            bool IsBlackedOut(DateOnly day) => blackoutDays.IsBlackoutDay(day);

            return new RefinementCalendarFacts(
                RefinementCadenceCalendar.NextAfter(cadence, today, IsBlackedOut),
                RefinementCadenceCalendar.IsCadenceDay(cadence, today, IsBlackedOut));
        }
    }
}
