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
                // from ForecastService. Keep those days so "done" still reads as done today (AC-02.3).
                // Team/TeamId stay null: the aggregate belongs to no single team (ADR-111).
                SetSimulationResult(joint.Count > 0 ? joint : DaysWithoutTrials(materialized));
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

        private static Dictionary<int, int> DaysWithoutTrials(IEnumerable<WhenForecast> forecasts)
        {
            return forecasts
                .SelectMany(f => f.SimulationResult.Keys)
                .Distinct()
                .ToDictionary(day => day, _ => 0);
        }
    }
}
