namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Metric families recorded by the process-behaviour-chart (NPL) over-time pipeline.
    /// Persisted as the integer ordinal — APPEND new members only. Reordering or
    /// renumbering silently re-maps every already-shipped snapshot row to a different family.
    /// Deliberately separate from <see cref="MetricType"/>: that enum names the percentile
    /// families, this one names the (larger, independently growing) process-behaviour families.
    /// </summary>
    public enum ProcessBehaviorMetricType
    {
        Throughput,
    }
}
