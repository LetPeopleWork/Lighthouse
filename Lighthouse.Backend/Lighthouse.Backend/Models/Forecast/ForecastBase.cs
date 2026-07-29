namespace Lighthouse.Backend.Models.Forecast
{
    public class ForecastBase
    {
        private SortedDictionary<int, int> simulationResult;
        private readonly IComparer<int> comparer;

        protected ForecastBase()
        {
        }

        protected ForecastBase(IComparer<int> comparer)
        {
            this.comparer = comparer;
        }

        public ForecastBase(Dictionary<int, int> simulationResult, IComparer<int> comparer)
        {
            CreationTime = DateTime.UtcNow;
            this.comparer = comparer;
            SetSimulationResult(simulationResult);
        }

        public int Id { get; set; }

        public int TotalTrials { get; set; }

        public DateTime CreationTime { get; set; }

        public List<IndividualSimulationResult> SimulationResults { get; set; } = new List<IndividualSimulationResult>();

        public SortedDictionary<int, int> SimulationResult
        {
            get
            {
                if (simulationResult == null || simulationResult.Count < 1)
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

        public double GetLikelihood(int threshold)
        {
            if (TotalTrials == 0)
            {
                return 0;
            }

            // The comparer carries the direction, so the threshold means "by day t" ascending and
            // "at least N items" descending (Bug #5586).
            var keyOrder = comparer ?? Comparer<int>.Default;

            var trialCounter = 0;

            foreach (var simulation in SimulationResult)
            {
                if (keyOrder.Compare(simulation.Key, threshold) > 0)
                {
                    break;
                }

                trialCounter += simulation.Value;
            }

            return 100 / (double)TotalTrials * trialCounter;
        }

        protected void SetSimulationResult(Dictionary<int, int> simulationResult)
        {
            SimulationResults.Clear();
            foreach (var item in simulationResult)
            {
                SimulationResults.Add(new IndividualSimulationResult { Key = item.Key, Value = item.Value, Forecast = this, ForecastId = Id });
            }

            TotalTrials = simulationResult.Values.Sum();
            this.simulationResult?.Clear();
        }
    }
}