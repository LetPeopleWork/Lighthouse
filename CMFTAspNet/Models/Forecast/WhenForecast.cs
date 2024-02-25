
namespace CMFTAspNet.Models.Forecast
{
    public class WhenForecast : ForecastBase
    {
        public WhenForecast() : base(Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {            
        }

        public WhenForecast(Dictionary<int, int> simulationResult, int numberOfItems) : base(simulationResult, Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {
            NumberOfItems = numberOfItems;
        }

        public int FeatureId { get; set; }

        public Feature Feature { get; set; }

        public int NumberOfItems { get; }

        public double GetLikelihood(int daysToTargetDate)
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

            return 100 / ((double)TotalTrials) * trialCounter;
        }
    }
}