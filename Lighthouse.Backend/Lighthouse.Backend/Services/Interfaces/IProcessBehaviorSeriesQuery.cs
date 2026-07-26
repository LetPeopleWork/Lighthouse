using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Read-only port (Epic 5427). Serves the persisted natural-process-limit series for an owner and
    /// process-behaviour metric family, ordered by RecordedAt ascending. Side-effect-free — it reads the
    /// snapshot table only and never triggers a recompute, so the chart re-plots exactly the days the
    /// recording pipeline judged honest enough to persist (US-01 AC5).
    /// Unlike the percentiles series there is no horizon dimension here, hence no sentinel to resolve.
    /// <paramref name="from"/>/<paramref name="to"/> are an optional window on RecordedAt, inclusive at
    /// both ends (slice-03b); narrowing plots fewer already-recorded days and still never recomputes.
    /// </summary>
    public interface IProcessBehaviorSeriesQuery
    {
        IReadOnlyList<ProcessBehaviorSnapshot> GetSeries(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType, DateOnly? from, DateOnly? to);
    }
}
