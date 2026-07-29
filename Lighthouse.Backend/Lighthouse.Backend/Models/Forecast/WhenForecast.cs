namespace Lighthouse.Backend.Models.Forecast
{
    public class WhenForecast : ForecastBase
    {
        public WhenForecast() : base(Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {
        }

        // Production seam for hand-built histograms: DeliveryCompletionForecast builds a per-team
        // carrier through it and Delivery its all-done day-0 marker (ADR-113 DDD-5). Added by Story
        // #5569 as a test seam only - do not delete it as scaffolding.
        internal WhenForecast(Dictionary<int, int> simulationResult) : this()
        {
            SetSimulationResult(simulationResult);
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

        public bool FilterApplied { get; set; }

        public string? ExcludedSummary { get; set; }

        public bool HasSufficientData { get; set; }
    }
}