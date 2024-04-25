namespace Lighthouse.Models.Forecast
{
    public class ForecastBase
    {
        private SortedDictionary<int, int> simulationResult;
        private readonly IComparer<int> comparer;

        protected ForecastBase() { }

        protected ForecastBase(IComparer<int> comparer)
        {
            this.comparer = comparer;
        }

        public ForecastBase(Dictionary<int, int> simulationResult, IComparer<int> comparer)
        {
            foreach (var item in simulationResult)
            {
                SimulationResults.Add(new IndividualSimulationResult { Key = item.Key, Value = item.Value, Forecast = this, ForecastId = Id });
            }

            TotalTrials = simulationResult.Values.Sum();
            CreationTime = DateTime.UtcNow;
            this.comparer = comparer;
        }

        public int Id { get; set; }

        public int TotalTrials { get; set; }

        public DateTime CreationTime { get; set; }

        public List<IndividualSimulationResult> SimulationResults { get; set; } = new List<IndividualSimulationResult>();

        public SortedDictionary<int, int> SimulationResult
        {
            get
            {
                if (simulationResult == null)
                {
                    var dictionary = new Dictionary<int, int>();
                    foreach (var item in SimulationResults)
                    {
                        dictionary.Add(item.Key, item.Value);
                    }

                    simulationResult = new SortedDictionary<int, int>(dictionary, comparer);
                }

                return simulationResult;
            }
        }

        public int GetProbability(int percentile)
        {
            var numberOfTrials = Math.Ceiling((double)TotalTrials / 100 * percentile);

            var trialCounter = 0;

            foreach (var key in SimulationResult.Keys)
            {
                trialCounter += SimulationResult[key];

                if (trialCounter >= numberOfTrials)
                {
                    return key;
                }
            }

            return -1;
        }
    }
}