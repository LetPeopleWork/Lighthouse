using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class Delivery : IConcurrencyTokenEntity
    {
        public Delivery(string name, DateTime date, int portfolioId, DateOnly today)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty");
            }

            if (DateOnly.FromDateTime(date) <= today)
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

        public DeliveryMetricsProjection CalculateMetrics(DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods, params int[] percentiles)
        {
            var featureBreakdown = CalculateFeatureBreakdown(today, blackoutPeriods);
            var governingFeature = GetGoverningFeature(today, blackoutPeriods);

            if (governingFeature == null)
            {
                return new DeliveryMetricsProjection(0.0, [], featureBreakdown);
            }

            // One un-forecastable feature makes the whole delivery un-forecastable - reporting the
            // governing feature's number would quietly ignore work that must still happen (ADR-112 D8).
            if (Features.Any(feature => !feature.CanBeForecast))
            {
                return new DeliveryMetricsProjection(null, [], featureBreakdown);
            }

            var whenDistribution = percentiles
                .Select(percentile => ToWhenPercentile(governingFeature.Forecast, percentile, today, blackoutPeriods))
                .ToList();

            return new DeliveryMetricsProjection(governingFeature.GetLikelhoodForDate(Date, today, blackoutPeriods), whenDistribution, featureBreakdown);
        }

        private List<DeliveryFeatureMetric> CalculateFeatureBreakdown(DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            return Features
                .Where(feature => feature.FeatureWork.Sum(work => work.TotalWorkItems) > 0)
                .Select(feature => ToFeatureMetric(feature, today, blackoutPeriods))
                .ToList();
        }

        private DeliveryFeatureMetric ToFeatureMetric(Feature feature, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var totalItems = feature.FeatureWork.Sum(work => work.TotalWorkItems);
            var remainingItems = feature.FeatureWork.Sum(work => work.RemainingWorkItems);
            var completion = Math.Clamp((double)(totalItems - remainingItems) / totalItems * 100.0, 0.0, 100.0);

            return new DeliveryFeatureMetric(feature.ReferenceId, feature.Name, completion, feature.GetLikelhoodForDate(Date, today, blackoutPeriods));
        }

        private Feature? GetGoverningFeature(DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            // A delivery finishes only when its latest feature finishes, so the governing feature - the one
            // whose forecast dates and likelihood represent the delivery - is the latest-completing one.
            // Ranking by likelihood alone saturates for large deliveries (every feature is 100% likely once
            // the target date is comfortably far out) and the tie-break then falls back to arbitrary
            // collection order, surfacing forecast dates earlier than individual features (ADO #5435).
            // Feature.Forecast is computed on every read and now runs the joint distribution, so the
            // likelihood and the forecast are each taken once per feature rather than per comparison.
            return Features
                .Select(feature => (feature, likelihood: feature.GetLikelhoodForDate(Date, today, blackoutPeriods)))
                .Where(candidate => candidate.likelihood >= 0)
                .Select(candidate => (candidate.feature, candidate.likelihood, forecast: candidate.feature.Forecast))
                .OrderByDescending(candidate => candidate.forecast.GetProbability(85))
                .ThenBy(candidate => candidate.likelihood)
                .Select(candidate => candidate.feature)
                .FirstOrDefault();
        }

        private static DeliveryWhenPercentile ToWhenPercentile(WhenForecast forecast, int percentile, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var expectedDate = blackoutPeriods.ProjectWorkingDays(InstanceCalendar.AsUtcMidnight(today), forecast.GetProbability(percentile));
            return new DeliveryWhenPercentile(percentile, expectedDate, forecast.FilterApplied, forecast.ExcludedSummary);
        }
    }
}