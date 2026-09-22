using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;

namespace Lighthouse.Backend.Services.Implementation
{
    public class OverTimeGapReconciler(ILighthouseClock clock, IOverTimeHistoryFiller filler) : IOverTimeGapReconciler
    {
        /// <summary>
        /// The most days one visit asks for. A year-wide picker then fills over successive loads
        /// rather than in one unbounded walk, and every day written stays written, so the next load
        /// carries on from where this one stopped.
        /// </summary>
        private const int MostDaysOneVisitAsksFor = 90;

        public void AskForTheDaysThatAreMissing(
            int ownerId,
            OwnerType ownerType,
            MetricType metricType,
            DateOnly? from,
            DateOnly? to,
            IReadOnlyList<PercentilesOverTimeSnapshot> daysAlreadyHeld)
        {
            // A request with no start bound is asking for the whole history. Where that history begins
            // is a property of the owner's stored items, and finding it out means a query on the thread
            // the reader is waiting on. So an open-ended request is served from what is stored and
            // nothing is asked for; narrowing the picker is what sets a walk going.
            if (from is null)
            {
                return;
            }

            var missing = DaysWithoutAReading(from.Value, to ?? clock.Today, daysAlreadyHeld);
            if (missing.Count == 0)
            {
                return;
            }

            filler.AskFor(new OverTimeFillRequest(ownerId, ownerType, metricType, missing));
        }

        private static List<DateOnly> DaysWithoutAReading(
            DateOnly from, DateOnly to, IReadOnlyList<PercentilesOverTimeSnapshot> daysAlreadyHeld)
        {
            var held = daysAlreadyHeld.Select(day => day.RecordedAt).ToHashSet();
            var missing = new List<DateOnly>();

            for (var day = from; day <= to && missing.Count < MostDaysOneVisitAsksFor; day = day.AddDays(1))
            {
                if (!held.Contains(day))
                {
                    missing.Add(day);
                }
            }

            return missing;
        }
    }
}
