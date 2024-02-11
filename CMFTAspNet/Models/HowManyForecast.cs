namespace CMFTAspNet.Models
{
    public class HowManyForecast
    {
        private readonly SortedDictionary<int, int> simulationResult;
        private readonly int totalTrials;

        public HowManyForecast(Dictionary<int, int> simulationResult)
        {
            this.simulationResult = new SortedDictionary<int, int>(simulationResult, Comparer<int>.Create((x, y) => y.CompareTo(x)));
            totalTrials = simulationResult.Values.Sum();
        }

        public int GetPercentile(int percentile)
        {
            var numberOfTrials = Math.Ceiling((double)totalTrials / 100 * percentile);

            var trialCounter = 0;

            foreach(var key in simulationResult.Keys)
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