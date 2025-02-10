
namespace Lighthouse.Backend.Models.Forecast
{
    public class WhenForecast : ForecastBase
    {
        public WhenForecast() : base(Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {            
        }

        public WhenForecast(SimulationResult simulationResult) : base(simulationResult.SimulationResults, Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {
            NumberOfItems = simulationResult.InitialRemainingItems;
            TeamId = simulationResult.Team?.Id;
            Team = simulationResult.Team;
        }

        public int FeatureId { get; set; }

        public Feature Feature { get; set; }

        public int? TeamId { get; set; }

        public Team? Team { get; set; }

        public int NumberOfItems { get; set; } = 0;
    }
}