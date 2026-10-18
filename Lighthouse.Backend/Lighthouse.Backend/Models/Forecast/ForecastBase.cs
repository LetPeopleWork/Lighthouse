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

        private IComparer<int> KeyOrder => comparer ?? Comparer<int>.Default;

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

                    simulationResult = new SortedDictionary<int, int>(dictionary, KeyOrder);
                }

                return simulationResult;
            }
        }

        /// <summary>The percentile as a number, -1 when it has no day; <see cref="GetPercentile"/> says which kind of none.</summary>
        public int GetProbability(int percentile) => GetPercentile(percentile).Day ?? -1;

        /// <summary>
        /// The histogram holds only the runs that finished, while every run simulated counts towards the
        /// total, so a percentile more runs must reach than finished at all lies beyond the horizon.
        /// </summary>
        public ForecastPercentile GetPercentile(int percentile)
        {
            var numberOfTrials = Math.Ceiling((double)TotalTrials / 100 * percentile);

            var trialCounter = 0;

            foreach (var (day, trials) in SimulationResult)
            {
                trialCounter += trials;

                if (trialCounter >= numberOfTrials)
                {
                    return ForecastPercentile.On(day);
                }
            }

            return TotalTrials > 0 ? ForecastPercentile.BeyondTheHorizon : ForecastPercentile.NoAnswer;
        }

        public double GetLikelihood(int threshold)
        {
            if (TotalTrials == 0)
            {
                return 0;
            }

            var trialCounter = 0;

            // KeyOrder carries the direction, so the threshold means "by day t" ascending and
            // "at least N items" descending (Bug #5586).
            foreach (var simulation in SimulationResult)
            {
                if (KeyOrder.Compare(simulation.Key, threshold) > 0)
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