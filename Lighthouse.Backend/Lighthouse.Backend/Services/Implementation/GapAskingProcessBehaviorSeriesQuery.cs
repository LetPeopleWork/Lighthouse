using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Answers with the limit series that is stored, then mentions which days of the requested window
    /// were not in it. Opening the chart is the only thing a delivery lead does, so opening the chart
    /// has to be what sets the missing days being worked out - but the answer goes back first and
    /// unchanged, and the asking costs the reader nothing.
    ///
    /// The same ask the percentile tabs make, under the same key, so the two charts fill together. A
    /// separate ask for limits would let a lead read a limits chart covering a different stretch of
    /// history from the percentile tabs beside it, with nothing on screen to say so.
    /// </summary>
    public class GapAskingProcessBehaviorSeriesQuery(
        ProcessBehaviorSeriesQuery servedFromWhatIsStored,
        IOverTimeGapReconciler reconciler) : IProcessBehaviorSeriesQuery
    {
        public IReadOnlyList<ProcessBehaviorSnapshot> GetSeries(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType, DateOnly? from, DateOnly? to)
        {
            var held = servedFromWhatIsStored.GetSeries(ownerId, ownerType, metricType, from, to);

            reconciler.AskForTheDaysThatAreMissing(ownerId, ownerType, from, to, [.. held.Select(day => day.RecordedAt)]);

            return held;
        }
    }
}
