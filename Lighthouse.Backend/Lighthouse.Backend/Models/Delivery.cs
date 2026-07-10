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
            var leastLikelyFeature = GetLeastLikelyFeature(blackoutPeriods);

            if (leastLikelyFeature == null)
            {
                return new DeliveryMetricsProjection(0.0, [], featureBreakdown);
            }

            var forecastingFeatures = Features
                .Where(feature => feature.FeatureWork.Sum(work => work.RemainingWorkItems) > 0)
                .ToList();

            if (forecastingFeatures.Count == 0)
            {
                forecastingFeatures.Add(leastLikelyFeature);
            }

            var whenDistribution = percentiles
                .Select(percentile => ToLatestWhenPercentile(forecastingFeatures, percentile, blackoutPeriods))
                .ToList();

            return new DeliveryMetricsProjection(leastLikelyFeature.GetLikelhoodForDate(Date, blackoutPeriods), whenDistribution, featureBreakdown);
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

        private Feature? GetLeastLikelyFeature(IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var rankedFeatures = Features
                .Select(feature => (feature, likelihood: feature.GetLikelhoodForDate(Date, blackoutPeriods)))
                .Where(ranked => ranked.likelihood >= 0)
                .OrderByDescending(ranked => ranked.likelihood)
                .ToList();

            if (rankedFeatures.Count == 0)
            {
                return null;
            }

            return rankedFeatures[^1].feature;
        }

        private static DeliveryWhenPercentile ToLatestWhenPercentile(IReadOnlyList<Feature> forecastingFeatures, int percentile, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            // A delivery finishes only when its latest feature finishes, so the delivery's date for a
            // given percentile is the latest of the contributing features' dates - never a single feature
            // chosen by likelihood (which saturates and picks arbitrarily for large deliveries, ADO #5435).
            return forecastingFeatures
                .Select(feature => ToWhenPercentile(feature.Forecast, percentile, blackoutPeriods))
                .MaxBy(when => when.ExpectedDate)!;
        }

        private static DeliveryWhenPercentile ToWhenPercentile(WhenForecast forecast, int percentile, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var expectedDate = blackoutPeriods.ProjectWorkingDays(DateTime.UtcNow.Date, forecast.GetProbability(percentile));
            return new DeliveryWhenPercentile(percentile, expectedDate, forecast.FilterApplied, forecast.ExcludedSummary);
        }
    }
}