using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Answers with the series that is stored, then mentions which days of the requested window were
    /// not in it. Opening the chart is the only thing a flow coach does, so opening the chart has to be
    /// what sets the missing days being worked out - but the answer goes back first and unchanged, and
    /// the asking costs the reader nothing.
    /// </summary>
    public class GapAskingPercentilesOverTimeSeriesQuery(
        PercentilesOverTimeSeriesQuery servedFromWhatIsStored,
        IOverTimeGapReconciler reconciler) : IPercentilesOverTimeSeriesQuery
    {
        public IReadOnlyList<PercentilesOverTimeSnapshot> GetSeries(int ownerId, OwnerType ownerType, MetricType metricType, int? horizon, DateOnly? from, DateOnly? to)
        {
            var held = servedFromWhatIsStored.GetSeries(ownerId, ownerType, metricType, horizon, from, to);

            reconciler.AskForTheDaysThatAreMissing(ownerId, ownerType, metricType, from, to, held);

            return held;
        }
    }
}
