namespace Lighthouse.Backend.Models.Forecast
{
    // One contributing (team, feature) work pair, joined to the per-team forecast row that covers it.
    // Forecast is null when the pair has remaining work but no Forecasts row at all - the C1 / DDD-7
    // shape, which must surface as "cannot forecast" and never as a silent CDF of 1.
    internal sealed record DeliveryForecastRow(int TeamId, Team? Team, WhenForecast? Forecast);

    // The composing builder for a delivery's completion distribution. It reimplements no maths:
    // contributing pairs -> bucket by team -> ComonotonicCompletionDistribution.Min within a bucket ->
    // a WhenForecast carrier per bucket -> AggregatedWhenForecast, which already runs the cross-bucket
    // product through JointCompletionDistribution. ADR-113 (Story #5587).
    //
    // It lives here rather than inside Delivery so the grain rule is machine-checkable
    // (Models.Delivery must not depend on either combinator) and so the mutation gate has a pure
    // target that needs no EF graph. Delivery keeps the guards - they are delivery policy, not
    // combination.
    //
    // __SCAFFOLD__ - DISTILL wrote the seam; DELIVER writes the bodies.
    internal static class DeliveryCompletionForecast
    {
        // Enumerated FROM FeatureWork.Where(RemainingWorkItems > 0), then LEFT JOINed to Forecasts -
        // never the reverse and never a cartesian product of teams x features (D10 / AC-01.6).
        // FeatureWork is the authoritative pair set; Forecasts is a derived, lagging projection of it.
        public static List<DeliveryForecastRow> ContributingRows(List<Feature> features)
        {
            throw new InvalidOperationException("__SCAFFOLD__ DeliveryCompletionForecast.ContributingRows is not implemented yet");
        }

        // Null when any contributing pair has no forecast row (C1 / DDD-7): the delivery reports
        // "cannot forecast" rather than quietly assuming that team's work is already done.
        public static AggregatedWhenForecast? Build(List<Feature> features)
        {
            throw new InvalidOperationException("__SCAFFOLD__ DeliveryCompletionForecast.Build is not implemented yet");
        }
    }
}
