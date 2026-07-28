namespace Lighthouse.Backend.Models
{
    // LikelihoodPercentage is null when the delivery cannot be forecast at all (ADR-112 D8).
    public sealed record DeliveryMetricsProjection(double? LikelihoodPercentage, IReadOnlyList<DeliveryWhenPercentile> WhenDistribution, IReadOnlyList<DeliveryFeatureMetric> FeatureBreakdown);

    public sealed record DeliveryWhenPercentile(int Percentile, DateTime ExpectedDate, bool FilterApplied, string? ExcludedSummary);

    public sealed record DeliveryFeatureMetric(string ReferenceId, string Name, double Completion, double? Likelihood);
}
