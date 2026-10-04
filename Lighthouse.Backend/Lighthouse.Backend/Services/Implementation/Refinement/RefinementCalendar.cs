using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public sealed class RefinementCalendar(ILighthouseClock clock) : IRefinementCalendar
    {
        public RefinementCalendarFacts FactsFor(RefinementCadence? cadence)
        {
            var today = clock.Today;

            return new RefinementCalendarFacts(
                RefinementCadenceCalendar.NextAfter(cadence, today),
                RefinementCadenceCalendar.IsCadenceDay(cadence, today));
        }
    }
}
