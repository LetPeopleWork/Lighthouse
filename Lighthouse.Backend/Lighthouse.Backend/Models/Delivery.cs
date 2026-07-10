using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class Delivery : IConcurrencyTokenEntity
    {
        public Delivery(string name, DateTime date, int portfolioId)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty");
            }

            if (date <= DateTime.UtcNow)
            {
                throw new ArgumentException("Delivery date must be in the future");
            }

            Name = name;
            Date = date;
            PortfolioId = portfolioId;
        }

        public Delivery()
        {
            Name = string.Empty;
        }

        public int Id { get; set; }

        public Guid ConcurrencyToken { get; set; }

        public string Name { get; set; }
        
        public DateTime Date { get; set; }
        
        public int PortfolioId { get; set; }
        
        public Portfolio? Portfolio { get; set; }
        
        public List<Feature> Features { get; } = [];

        public DeliverySelectionMode SelectionMode { get; set; } = DeliverySelectionMode.Manual;

        public string? RuleDefinitionJson { get; set; }

        public int? RuleSchemaVersion { get; set; }

        public DeliveryMetricsProjection CalculateMetrics(IReadOnlyList<BlackoutPeriod> blackoutPeriods, params int[] percentiles)
        {
            var featureBreakdown = CalculateFeatureBreakdown(blackoutPeriods);
            var governingFeature = GetGoverningFeature(blackoutPeriods);

            if (governingFeature == null)
            {
                return new DeliveryMetricsProjection(0.0, [], featureBreakdown);
            }

            var whenDistribution = percentiles
                .Select(percentile => ToWhenPercentile(governingFeature.Forecast, percentile, blackoutPeriods))
                .ToList();

            return new DeliveryMetricsProjection(governingFeature.GetLikelhoodForDate(Date, blackoutPeriods), whenDistribution, featureBreakdown);
        }

        private List<DeliveryFeatureMetric> CalculateFeatureBreakdown(IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            return Features
                .Where(feature => feature.FeatureWork.Sum(work => work.TotalWorkItems) > 0)
                .Select(feature => ToFeatureMetric(feature, blackoutPeriods))
                .ToList();
        }

        private DeliveryFeatureMetric ToFeatureMetric(Feature feature, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var totalItems = feature.FeatureWork.Sum(work => work.TotalWorkItems);
            var remainingItems = feature.FeatureWork.Sum(work => work.RemainingWorkItems);
            var completion = Math.Clamp((double)(totalItems - remainingItems) / totalItems * 100.0, 0.0, 100.0);

            return new DeliveryFeatureMetric(feature.ReferenceId, feature.Name, completion, feature.GetLikelhoodForDate(Date, blackoutPeriods));
        }

        private Feature? GetGoverningFeature(IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            // A delivery finishes only when its latest feature finishes, so the governing feature - the one
            // whose forecast dates and likelihood represent the delivery - is the latest-completing one.
            // Ranking by likelihood alone saturates for large deliveries (every feature is 100% likely once
            // the target date is comfortably far out) and the tie-break then falls back to arbitrary
            // collection order, surfacing forecast dates earlier than individual features (ADO #5435).
            return Features
                .Where(feature => feature.GetLikelhoodForDate(Date, blackoutPeriods) >= 0)
                .OrderByDescending(feature => feature.Forecast.GetProbability(85))
                .ThenBy(feature => feature.GetLikelhoodForDate(Date, blackoutPeriods))
                .FirstOrDefault();
        }

        private static DeliveryWhenPercentile ToWhenPercentile(WhenForecast forecast, int percentile, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var expectedDate = blackoutPeriods.ProjectWorkingDays(DateTime.UtcNow.Date, forecast.GetProbability(percentile));
            return new DeliveryWhenPercentile(percentile, expectedDate, forecast.FilterApplied, forecast.ExcludedSummary);
        }
    }
}