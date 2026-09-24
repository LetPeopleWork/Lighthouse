using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;

namespace Lighthouse.Backend.Services.Implementation
{
    public class OverTimeGapReconciler(
        ILighthouseClock clock,
        IOverTimeHistoryFiller filler,
        ReconstructionMemo memo,
        IOverTimeHistoryFillSwitch fillSwitch) : IOverTimeGapReconciler
    {
        /// <summary>
        /// The most days one visit hands over, and deliberately not the most a pass will work out.
        /// Which of these days the owner's history can support is only known once a pass has looked,
        /// so handing over just the first few would, for a period reaching back past where that history
        /// begins, hand over nothing but days no pass can write - and the part of the period the history
        /// does cover would not arrive. The pass steps over the unsupported days for free and caps the
        /// ones it actually works out. This bound only stops a hand-typed range spanning centuries from
        /// queueing centuries of days, so it sits well past any period someone would actually open.
        /// </summary>
        private const int MostDaysOneVisitHandsOver = 10 * 366;

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

            // A period may be picked to end after today, but no day after today has happened yet, so
            // none of those days can be missing.
            var today = clock.Today;
            var lastDay = to is { } pickedEnd && pickedEnd < today ? pickedEnd : today;

            var missing = DaysWithoutAReading(ownerId, ownerType, from.Value, lastDay, daysAlreadyHeld);
            // Asked only once days are known to be missing, so a chart that already holds its whole
            // period pays nothing for the switch. Switched off says nothing to the log: that would be a
            // line on every chart load of every instance that simply left the preview off.
            if (missing.Count == 0 || !fillSwitch.IsSwitchedOn())
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

            for (var day = from; day <= to && missing.Count < MostDaysOneVisitHandsOver; day = day.AddDays(1))
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
