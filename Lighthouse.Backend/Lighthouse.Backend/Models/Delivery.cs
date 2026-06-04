using Lighthouse.Backend.Models.Forecast;
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

        public DeliveryMetricsProjection CalculateMetrics(params int[] percentiles)
        {
            var leastLikelyFeature = GetLeastLikelyFeature();

            if (leastLikelyFeature == null)
            {
                return new DeliveryMetricsProjection(0.0, []);
            }

            var whenDistribution = percentiles
                .Select(percentile => ToWhenPercentile(leastLikelyFeature.Forecast, percentile))
                .ToList();

            return new DeliveryMetricsProjection(leastLikelyFeature.GetLikelhoodForDate(Date), whenDistribution);
        }

        private Feature? GetLeastLikelyFeature()
        {
            var rankedFeatures = Features
                .Where(feature => feature.GetLikelhoodForDate(Date) >= 0)
                .OrderByDescending(feature => feature.GetLikelhoodForDate(Date))
                .ToList();

            if (rankedFeatures.Count == 0)
            {
                return null;
            }

            return rankedFeatures[^1];
        }

        private static DeliveryWhenPercentile ToWhenPercentile(WhenForecast forecast, int percentile)
        {
            var expectedDate = DateTime.UtcNow.Date.AddDays(forecast.GetProbability(percentile));
            return new DeliveryWhenPercentile(percentile, expectedDate, forecast.FilterApplied, forecast.ExcludedSummary);
        }
    }
}