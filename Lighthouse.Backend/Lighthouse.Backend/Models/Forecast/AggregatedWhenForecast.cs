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

            var worstCase = materialized.MaxBy(f => f.GetProbability(85));
            if (worstCase != null)
            {
                SetSimulationResult(new Dictionary<int, int>(worstCase.SimulationResult));
                Team = worstCase.Team;
                TeamId = worstCase.TeamId;
                NumberOfItems = worstCase.NumberOfItems;
                CreationTime = worstCase.CreationTime;
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
