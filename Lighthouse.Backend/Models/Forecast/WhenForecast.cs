
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

        public virtual double GetLikelihood(int daysToTargetDate)
        {
            var trialCounter = 0;

            foreach (var simulation in SimulationResult)
            {
                trialCounter += simulation.Value;

                if (simulation.Key >= daysToTargetDate)
                {
                    break;
                }
            }

            if (trialCounter > 0)
            {
                return 100 / ((double)TotalTrials) * trialCounter;
            }

            return 100;
        }
    }
}