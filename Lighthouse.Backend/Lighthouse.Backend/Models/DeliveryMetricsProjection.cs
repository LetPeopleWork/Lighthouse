namespace Lighthouse.Backend.Models
{
    public sealed record DeliveryMetricsProjection(double LikelihoodPercentage, IReadOnlyList<DeliveryWhenPercentile> WhenDistribution);

    public sealed record DeliveryWhenPercentile(int Percentile, DateTime ExpectedDate, bool FilterApplied, string? ExcludedSummary);
}
