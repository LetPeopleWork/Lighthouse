using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Models.History
{
    public class WhenForecastHistoryEntry : ForecastBase
    {
        public WhenForecastHistoryEntry()
        {            
        }

        public WhenForecastHistoryEntry(WhenForecast whenForecast, FeatureHistoryEntry featureHistoryEntry) : base(Comparer<int>.Create((x, y) => x.CompareTo(y)))
        {
            FeatureHistoryEntry = featureHistoryEntry;
            FeatureHistoryEntryId = featureHistoryEntry.Id;
            TeamId = whenForecast.TeamId ?? 0;
            NumberOfItems = whenForecast.NumberOfItems;

            SetSimulationResult(new Dictionary<int, int>(whenForecast.SimulationResult));
        }

        public int FeatureHistoryEntryId { get; set; }

        public FeatureHistoryEntry FeatureHistoryEntry { get; set; }

        public int TeamId {  get; set; }

        public int NumberOfItems { get; set; }

    }
}
