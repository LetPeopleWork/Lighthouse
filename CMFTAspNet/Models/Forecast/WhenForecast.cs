
namespace CMFTAspNet.Models.Forecast
{
    public class WhenForecast : ForecastBase
    {
        public WhenForecast(Dictionary<int, int> simulationResult) : base(simulationResult, Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {
        }

        public double GetLikelihood(int daysToTargetDate)
        {
            var trialCounter = 0;

            foreach (var simulation in simulationResult)
            {
                trialCounter += simulation.Value;

                if (simulation.Key >= daysToTargetDate)
                {
                    break;
                }
            }

            return 100 / ((double)totalTrials) * trialCounter;
        }
    }
}