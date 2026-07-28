namespace Lighthouse.Backend.Models.Forecast
{
    public class AggregatedWhenForecast : WhenForecast
    {
        public AggregatedWhenForecast()
        {
        }

        public AggregatedWhenForecast(IEnumerable<WhenForecast> forecasts) : base()
        {
            var materialized = forecasts.ToList();

            if (materialized.Count > 0)
            {
                var joint = JointCompletionDistribution.Combine(materialized.Select(f => f.SimulationResult));

                // No contributor carries trials - a feature with no remaining work gets the day-0 sentinel
                // from ForecastService. Keep it: that is a fact, not a forecast (AC-02.3).
                // Team/TeamId stay null: the aggregate belongs to no single team (ADR-111).
                SetSimulationResult(joint.Count > 0 ? joint : new Dictionary<int, int>(materialized[0].SimulationResult));
                NumberOfItems = materialized.Sum(f => f.NumberOfItems);
                CreationTime = materialized.Min(f => f.CreationTime);
            }

            FilterApplied = materialized.Any(f => f.FilterApplied);
            HasSufficientData = materialized.Count == 0 || materialized.All(f => f.HasSufficientData);
            var summaries = materialized
                .Where(f => !string.IsNullOrWhiteSpace(f.ExcludedSummary))
                .Select(f => f.ExcludedSummary!)
                .Distinct()
                .ToList();
            ExcludedSummary = summaries.Count == 0 ? null : string.Join("; ", summaries);
        }
    }
}
