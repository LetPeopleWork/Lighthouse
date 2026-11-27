using Lighthouse.Backend.Modules.FlowMetrics.Domain;

namespace Lighthouse.Backend.Models.Metrics
{
    public sealed record WithinSleInfoDto(
        WithinSleVerdict Verdict,
        int ClosedCount,
        int WithinCount,
        int? SlePercentile,
        int? SleDays,
        double? SharePercent,
        InfoWidgetComparisonDto Comparison)
    {
        public static WithinSleInfoDto From(WithinSleShare share, InfoWidgetComparisonDto comparison)
        {
            throw new NotImplementedException("The Within SLE answer is not built from a share yet.");
        }
    }
}
