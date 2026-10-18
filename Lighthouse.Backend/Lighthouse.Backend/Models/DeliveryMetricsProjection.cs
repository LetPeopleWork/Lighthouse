namespace Lighthouse.Backend.Models
{
    // LikelihoodPercentage is null when the delivery cannot be forecast at all.
    // HasSufficientData has no default on purpose: it composes with the un-forecastable state rather
    // than being superseded by it, so every return path must answer it.
    public sealed record DeliveryMetricsProjection(double? LikelihoodPercentage, IReadOnlyList<DeliveryWhenPercentile> WhenDistribution, IReadOnlyList<DeliveryFeatureMetric> FeatureBreakdown, bool HasSufficientData);

    public sealed record DeliveryWhenPercentile(int Percentile, DateTime ExpectedDate, bool FilterApplied, string? ExcludedSummary)
    {
        public bool BeyondHorizon { get; init; }

        public int HorizonYears { get; init; }
    }

    // TotalItems / IsUsingDefaultSize are optional so snapshots recorded before they existed keep
    // deserialising without a backfill.
    public sealed record DeliveryFeatureMetric(string ReferenceId, string Name, double Completion, double? Likelihood)
    {
        public int? TotalItems { get; init; }

        public bool? IsUsingDefaultSize { get; init; }

        /// <summary>
        /// Where the Feature lived in the work tracking system, kept so a closed Delivery's list is
        /// still something a reader can click through. Optional for the same reason as the two
        /// above: records written before it existed keep deserialising, and render unlinked.
        /// </summary>
        public string? Url { get; init; }
    }
}
