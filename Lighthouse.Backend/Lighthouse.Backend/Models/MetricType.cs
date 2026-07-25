namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Metric families recorded by the percentiles-over-time pipeline.
    /// Persisted as the integer ordinal — APPEND new members only. Reordering or
    /// renumbering silently re-maps every already-shipped snapshot row to a different family.
    /// </summary>
    public enum MetricType
    {
        CycleTime,
        WorkItemAge,
    }
}
