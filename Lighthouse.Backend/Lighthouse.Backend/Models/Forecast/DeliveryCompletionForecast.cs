namespace Lighthouse.Backend.Models.Forecast
{
    // A contributing (team, feature) pair joined to the forecast row covering it. Forecast is null when
    // the pair has remaining work but no row at all, which must surface as "cannot forecast".
    internal sealed record DeliveryForecastRow(int TeamId, WhenForecast? Forecast);

    // Composes a delivery's completion distribution from the existing combinators, reimplementing no
    // maths. Separate from Delivery so the grain rule is machine-checkable and the mutation gate has a
    // target needing no EF graph; Delivery keeps the guards.
    internal static class DeliveryCompletionForecast
    {
        // Enumerated FROM FeatureWork.Where(RemainingWorkItems > 0), then LEFT JOINed to Forecasts -
        // never the reverse and never a cartesian product of teams x features.
        // FeatureWork is the authoritative pair set; Forecasts is a derived, lagging projection of it.
        public static List<DeliveryForecastRow> ContributingRows(List<Feature> features)
        {
            return features.SelectMany(RowsFor).ToList();
        }

        // Null when any contributing pair has no forecast row: the delivery reports
        // "cannot forecast" rather than quietly assuming that team's work is already done.
        public static AggregatedWhenForecast? Build(List<Feature> features)
        {
            var rows = ContributingRows(features);

            // A row with no trials is the same gap as no row: Min drops it, which would turn that team
            // into a certainty. Feature.TeamsWithoutForecast catches it first only when it can NAME the
            // team, and that depends on the caller's EF includes - so re-derive it here from the rows.
            if (rows.Exists(row => row.Forecast is null || row.Forecast.TotalTrials == 0))
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
                .Select(work => new DeliveryForecastRow(work.TeamId, ForecastRowFor(feature, work)));
        }

        // The Team?.Id ?? TeamId precedence matches Feature.TeamFor. The whole-feature day-0 sentinel
        // has both null, so it matches no pair and a null-keyed bucket is unrepresentable.
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

            // The carrier keeps the runs its Team did not finish as a histogram entry of their own, so
            // it reaches the aggregate across Teams holding every run rather than only the finished ones.
            return new WhenForecast(ComonotonicCompletionDistribution.Min(contributors.Select(WhenForecast.EveryRunOf)))
            {
                NumberOfItems = contributors.Sum(forecast => forecast.NumberOfItems),
                CreationTime = contributors.Min(forecast => forecast.CreationTime),
                FilterApplied = contributors.Exists(forecast => forecast.FilterApplied),
                ExcludedSummary = summaries.Count == 0 ? null : string.Join("; ", summaries),
            };
        }
    }
}
