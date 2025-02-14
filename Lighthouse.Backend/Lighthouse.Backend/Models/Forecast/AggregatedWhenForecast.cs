namespace Lighthouse.Backend.Models.Forecast
{
    public class AggregatedWhenForecast : WhenForecast
    {
        public AggregatedWhenForecast()
        {
        }

        public AggregatedWhenForecast(IEnumerable<WhenForecast> forecasts) : base()
        {
            var worstCaseForecast = int.MinValue;

            foreach (var forecast in forecasts)
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
        }
    }
}
