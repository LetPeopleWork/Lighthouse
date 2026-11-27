namespace Lighthouse.Backend.Models.Metrics
{
    public sealed record WithinSleInfoDto(
        WithinSleVerdict Verdict,
        int ClosedCount,
        int WithinCount,
        int? SlePercentile,
        int? SleDays,
        double? SharePercent,
        InfoWidgetComparisonDto Comparison,
        int MinimumClosed);
}
