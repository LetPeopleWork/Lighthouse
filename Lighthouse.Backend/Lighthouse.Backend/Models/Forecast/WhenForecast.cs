namespace Lighthouse.Backend.Models.Forecast
{
    public class WhenForecast : ForecastBase
    {
        public WhenForecast() : base(Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {
        }

        // Production seam for hand-built histograms: DeliveryCompletionForecast builds a per-team
        // carrier through it and Delivery its all-done day-0 marker. It began as a test seam, so it is
        // easy to mistake for scaffolding - it is not.
        internal WhenForecast(Dictionary<int, int> simulationResult) : this()
        {
            SetSimulationResult(simulationResult);
        }

        public WhenForecast(SimulationResult simulationResult) : base(simulationResult.SimulationResults, Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {
            TotalTrials = Math.Max(TotalTrials, simulationResult.RunsSimulated);
            NumberOfItems = simulationResult.InitialRemainingItems;
            TeamId = simulationResult.Team?.Id;
            Team = simulationResult.Team;
        }

        public int FeatureId { get; set; }

        public Feature Feature { get; set; }

        public int? TeamId { get; set; }

        public Team? Team { get; set; }

        public int NumberOfItems { get; set; } = 0;

        public bool FilterApplied { get; set; }

        public string? ExcludedSummary { get; set; }

        public bool HasSufficientData { get; set; }

        // Later than any day, so the combinators, which read days in ascending order, treat a run that
        // never finished as finishing after every run that did.
        internal const int NeverFinished = int.MaxValue;

        /// <summary>
        /// A forecast's histogram holds only the runs that finished. Combining histograms alone would
        /// drop the rest and turn a Team that falls short into one that always finishes, so the runs it
        /// did not finish go in too, on a day later than any other.
        /// </summary>
        internal static Dictionary<int, int> EveryRunOf(WhenForecast forecast)
        {
            var everyRun = forecast.SimulationResult.ToDictionary(day => day.Key, day => day.Value);
            var unfinished = forecast.TotalTrials - everyRun.Values.Sum();

            if (unfinished > 0)
            {
                everyRun[NeverFinished] = everyRun.GetValueOrDefault(NeverFinished) + unfinished;
            }

            return everyRun;
        }
    }
}