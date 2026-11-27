using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public sealed record ForecastThroughputStatus(RunChartData Throughput, bool FilterApplied, string? ExcludedSummary, bool HasSufficientData = true);
}
