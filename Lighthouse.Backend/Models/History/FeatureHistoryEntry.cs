using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Models.History
{
    [Index(nameof(FeatureReferenceId), nameof(Snapshot))]
    public class FeatureHistoryEntry : IEntity
    {
        public FeatureHistoryEntry()
        {
        }
        public FeatureHistoryEntry(Feature feature)
        {
            Update(feature);
        }

        public int Id { get; set; }

        public int FeatureId { get; set; }

        public string FeatureReferenceId { get; set; }

        public DateOnly Snapshot { get; set; }

        public List<FeatureWorkHistoryEntry> FeatureWork { get; } = [];

        public List<WhenForecastHistoryEntry> Forecasts { get; } = [];

        public void Update(Feature feature)
        {
            FeatureId = feature.Id;
            FeatureReferenceId = feature.ReferenceId;
            Snapshot = DateOnly.FromDateTime(DateTime.Today);

            FeatureWork.Clear();
            Forecasts.Clear();

            foreach (var featureWork in feature.FeatureWork)
            {
                FeatureWork.Add(new FeatureWorkHistoryEntry(featureWork, this));
            }

            foreach (var forecast in feature.Forecasts)
            {
                Forecasts.Add(new WhenForecastHistoryEntry(forecast, this));
            }
        }
    }
}
