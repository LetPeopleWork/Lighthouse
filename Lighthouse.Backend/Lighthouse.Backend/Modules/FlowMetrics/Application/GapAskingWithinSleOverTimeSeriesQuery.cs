using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>
    /// Answers with the Within SLE record that is stored, then mentions which days of the requested window were not
    /// in it, so opening the chart is what sets the missing days being filled in. The answer goes back first and
    /// unchanged, and the asking costs the reader nothing.
    /// </summary>
    public sealed class GapAskingWithinSleOverTimeSeriesQuery(
        WithinSleOverTimeSeriesQuery servedFromWhatIsStored,
        IOverTimeGapReconciler reconciler) : IWithinSleOverTimeSeriesQuery
    {
        public WithinSleOverTimeDto GetSeries(int ownerId, OwnerType ownerType, int horizon, DateOnly? from, DateOnly? to)
        {
            var held = servedFromWhatIsStored.GetSeries(ownerId, ownerType, horizon, from, to);

            reconciler.AskForTheDaysThatAreMissing(ownerId, ownerType, from, to, [.. held.Days.Select(day => day.RecordedAt)]);

            return held;
        }
    }
}
