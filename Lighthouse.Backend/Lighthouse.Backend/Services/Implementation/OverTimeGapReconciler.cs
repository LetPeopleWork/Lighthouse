using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;

namespace Lighthouse.Backend.Services.Implementation
{
    public class OverTimeGapReconciler(
        ILighthouseClock clock, IOverTimeHistoryFiller filler, ReconstructionMemo memo) : IOverTimeGapReconciler
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
            DateOnly? from,
            DateOnly? to,
            IReadOnlyList<DateOnly> daysAlreadyHeld)
        {
            // A request with no start bound is asking for the whole history. Where that history begins
            // is a property of the owner's stored items, and finding it out means a query on the thread
            // the reader is waiting on. So an open-ended request is served from what is stored and
            // nothing is asked for; narrowing the picker is what sets a walk going.
            if (from is null)
            {
                return;
            }

            var missing = DaysWithoutAReading(ownerId, ownerType, from.Value, to ?? clock.Today, daysAlreadyHeld);
            if (missing.Count == 0)
            {
                return;
            }

            filler.AskFor(new OverTimeFillRequest(ownerId, ownerType, missing));
        }

        /// <summary>
        /// The days come from whichever chart noticed them, and the pass they are handed to fills every
        /// chart the owner has. That is deliberate and self-correcting: a day the other chart already
        /// holds is stepped over by the writer, and a day only the other chart is missing is noticed the
        /// next time that chart is opened.
        /// </summary>
        private List<DateOnly> DaysWithoutAReading(
            int ownerId,
            OwnerType ownerType,
            DateOnly from,
            DateOnly to,
            IReadOnlyList<DateOnly> daysAlreadyHeld)
        {
            var held = daysAlreadyHeld.ToHashSet();
            var missing = new List<DateOnly>();

            for (var day = from; day <= to && missing.Count < MostDaysOneVisitAsksFor; day = day.AddDays(1))
            {
                // A day an earlier pass established cannot be written is left out, or it is asked for
                // again on every load for as long as the instance runs and looking at a settled period
                // never gets any cheaper. This is a dictionary lookup and nothing else - whatever it
                // took to find that out was paid once, in a pass, off this thread.
                if (held.Contains(day) || memo.NoPassCanWrite(ownerId, ownerType, day))
                {
                    continue;
                }

                missing.Add(day);
            }

            return missing;
        }
    }
}
