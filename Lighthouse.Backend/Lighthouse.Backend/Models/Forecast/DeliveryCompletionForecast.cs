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
    internal static class DeliveryCompletionForecast
    {
        // Enumerated FROM FeatureWork.Where(RemainingWorkItems > 0), then LEFT JOINed to Forecasts -
        // never the reverse and never a cartesian product of teams x features (D10 / AC-01.6).
        // FeatureWork is the authoritative pair set; Forecasts is a derived, lagging projection of it.
        public static List<DeliveryForecastRow> ContributingRows(List<Feature> features)
        {
            return features.SelectMany(RowsFor).ToList();
        }

        // Null when any contributing pair has no forecast row (C1 / DDD-7): the delivery reports
        // "cannot forecast" rather than quietly assuming that team's work is already done.
        public static AggregatedWhenForecast? Build(List<Feature> features)
        {
            var rows = ContributingRows(features);

            if (rows.Exists(row => row.Forecast is null))
            {
                return null;
            }

            var teamForecasts = rows
                .GroupBy(row => row.TeamId)
                .Select(CarrierFor)
                .ToList();

            return new AggregatedWhenForecast(teamForecasts);
        }

        // Method groups rather than inline lambdas: CS9236 fires on Sonar when the same nested generic
        // lambda has to be bound repeatedly.
        private static IEnumerable<DeliveryForecastRow> RowsFor(Feature feature)
        {
            return feature.FeatureWork
                .Where(work => work.RemainingWorkItems > 0)
                .Select(work => new DeliveryForecastRow(work.TeamId, work.Team, ForecastRowFor(feature, work)));
        }

        // The Team?.Id ?? TeamId precedence matches Feature.TeamFor. The whole-feature day-0 sentinel
        // has both null, so it matches no pair and a null-keyed bucket is unrepresentable (AC-01.8).
        private static WhenForecast? ForecastRowFor(Feature feature, FeatureWork work)
        {
            return feature.Forecasts.FirstOrDefault(forecast => (forecast.Team?.Id ?? forecast.TeamId) == work.TeamId);
        }

        // Within a bucket the metadata composes exactly as AggregatedWhenForecast composes it across
        // buckets, so a delivery whose teams work more than one feature keeps its filter indicator.
        private static WhenForecast CarrierFor(IGrouping<int, DeliveryForecastRow> bucket)
        {
            var contributors = bucket.Select(row => row.Forecast!).ToList();

            var summaries = contributors
                .Where(forecast => !string.IsNullOrWhiteSpace(forecast.ExcludedSummary))
                .Select(forecast => forecast.ExcludedSummary!)
                .Distinct()
                .ToList();

            return new WhenForecast(ComonotonicCompletionDistribution.Min(contributors.Select(forecast => forecast.SimulationResult)))
            {
                NumberOfItems = contributors.Sum(forecast => forecast.NumberOfItems),
                CreationTime = contributors.Min(forecast => forecast.CreationTime),
                FilterApplied = contributors.Exists(forecast => forecast.FilterApplied),
                ExcludedSummary = summaries.Count == 0 ? null : string.Join("; ", summaries),
                HasSufficientData = contributors.TrueForAll(forecast => forecast.HasSufficientData),
            };
        }
    }
}
