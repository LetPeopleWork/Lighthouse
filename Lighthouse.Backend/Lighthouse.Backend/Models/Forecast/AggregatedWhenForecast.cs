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
            var worstCaseForecast = int.MinValue;

            foreach (var forecast in materialized)
            {
                var result = forecast.GetProbability(85);

                if (result > worstCaseForecast)
                {
                    worstCaseForecast = result;

                    SetSimulationResult(new Dictionary<int, int>(forecast.SimulationResult));
                    Team = forecast.Team;
                    TeamId = forecast.TeamId;
                    NumberOfItems = forecast.NumberOfItems;
                    CreationTime = forecast.CreationTime;
                }
            }

            FilterApplied = materialized.Any(f => f.FilterApplied);
            var summaries = materialized
                .Where(f => !string.IsNullOrWhiteSpace(f.ExcludedSummary))
                .Select(f => f.ExcludedSummary!)
                .Distinct()
                .ToList();
            ExcludedSummary = summaries.Count == 0 ? null : string.Join("; ", summaries);
        }
    }
}
