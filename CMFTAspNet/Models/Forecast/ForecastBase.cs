namespace CMFTAspNet.Models.Forecast
{
    public class ForecastBase
    {
        protected readonly int totalTrials;

        protected readonly SortedDictionary<int, int> simulationResult;

        public ForecastBase(Dictionary<int, int> simulationResult, IComparer<int> comparer)
        {
            this.simulationResult = new SortedDictionary<int, int>(simulationResult, comparer);
            
            totalTrials = simulationResult.Values.Sum();
        }

        public int GetProbability(int percentile)
        {
            var numberOfTrials = Math.Ceiling((double)totalTrials / 100 * percentile);

            var trialCounter = 0;

            foreach (var key in simulationResult.Keys)
            {
                trialCounter += simulationResult[key];

                if (trialCounter >= numberOfTrials)
                {
                    return key;
                }
            }

            return -1;
        }
    }
}