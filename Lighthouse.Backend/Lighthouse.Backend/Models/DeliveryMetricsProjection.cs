namespace Lighthouse.Backend.Models
{
    // LikelihoodPercentage is null when the delivery cannot be forecast at all (ADR-112 D8).
    // HasSufficientData has no default on purpose: it composes with the un-forecastable state rather
    // than being superseded by it, so every return path must answer it (ADR-113 AC-02.5).
    public sealed record DeliveryMetricsProjection(double? LikelihoodPercentage, IReadOnlyList<DeliveryWhenPercentile> WhenDistribution, IReadOnlyList<DeliveryFeatureMetric> FeatureBreakdown, bool HasSufficientData);

    public sealed record DeliveryWhenPercentile(int Percentile, DateTime ExpectedDate, bool FilterApplied, string? ExcludedSummary);

    public sealed record DeliveryFeatureMetric(string ReferenceId, string Name, double Completion, double? Likelihood);
}
