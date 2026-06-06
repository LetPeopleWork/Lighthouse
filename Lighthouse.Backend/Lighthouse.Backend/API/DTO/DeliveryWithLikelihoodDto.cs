using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureLikelihoodDto
    {
        public int FeatureId { get; set; }

        public double LikelihoodPercentage { get; set; }

        public List<WhenForecastDto> CompletionDates { get; set; } = [];

        public bool HasSufficientData { get; set; } = true;
    }

    public class DeliveryWithLikelihoodDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public int PortfolioId { get; set; }

        public double LikelihoodPercentage { get; set; }

        public List<WhenForecastDto> CompletionDates { get; set; } = [];

        public double Progress { get; set; }

        public int RemainingWork { get; set; }

        public int TotalWork { get; set; }

        public List<int> Features { get; set; } = [];

        public List<FeatureLikelihoodDto> FeatureLikelihoods { get; set; } = [];

        public bool HasSufficientData { get; set; } = true;

        public int MetricSnapshotCount { get; set; }

        public DeliverySelectionMode SelectionMode { get; set; }

        public List<WorkItemRuleCondition> Rules { get; set; } = [];

        public string Mode { get; set; } = WorkItemRuleSet.ModeAnd;

        public Guid ConcurrencyToken { get; set; }

        public static DeliveryWithLikelihoodDto FromDelivery(Delivery delivery, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var featureLikelihoods = CalculateFeatureLikelihoods(delivery, blackoutPeriods);

            var metrics = delivery.CalculateMetrics(blackoutPeriods, 70, 85, 95);
            var completionDates = metrics.WhenDistribution.Select(ToWhenForecastDto).ToList();

            var leastLikelyFeature = GetLeastLikelyFeature(featureLikelihoods);

            var (progress, remainingWork, totalWork) = CalculateDeliveryWork(delivery);

            return new DeliveryWithLikelihoodDto
            {
                Id = delivery.Id,
                Name = delivery.Name,
                Date = delivery.Date,
                PortfolioId = delivery.PortfolioId,
                LikelihoodPercentage = metrics.LikelihoodPercentage,
                CompletionDates = completionDates,
                Progress = progress,
                RemainingWork = remainingWork,
                TotalWork = totalWork,
                Features = delivery.Features.Select(f => f.Id).ToList(),
                FeatureLikelihoods = featureLikelihoods,
                HasSufficientData = leastLikelyFeature?.HasSufficientData ?? true,
                SelectionMode = delivery.SelectionMode,
                Rules = GetRuleSet(delivery.RuleDefinitionJson).Conditions,
                Mode = GetRuleSet(delivery.RuleDefinitionJson).Mode,
                ConcurrencyToken = delivery.ConcurrencyToken,
            };
        }

        private static WhenForecastDto ToWhenForecastDto(DeliveryWhenPercentile percentile)
        {
            return new WhenForecastDto
            {
                Probability = percentile.Percentile,
                ExpectedDate = percentile.ExpectedDate,
                FilterApplied = percentile.FilterApplied,
                ExcludedSummary = percentile.ExcludedSummary,
            };
        }

        private static WorkItemRuleSet GetRuleSet(string? ruleDefinitionJson)
        {
            if (string.IsNullOrEmpty(ruleDefinitionJson))
            {
                return new WorkItemRuleSet();
            }

            return JsonSerializer.Deserialize<WorkItemRuleSet>(ruleDefinitionJson, JsonOptions) ?? new WorkItemRuleSet();
        }

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private static FeatureLikelihoodDto? GetLeastLikelyFeature(List<FeatureLikelihoodDto> featureLikelihoods)
        {
            var likelihoods = featureLikelihoods
                .Where(fl => fl.LikelihoodPercentage >= 0)
                .OrderByDescending(fl => fl.LikelihoodPercentage)
                .ToList();

            // Return 0 if no features have forecasts
            if (likelihoods.Count == 0)
            {
                return null;
            }

            // Return the minimum likelihood (most conservative estimate)
            return likelihoods[^1];
        }

        private static (double progress, int remainingWork, int totalWork) CalculateDeliveryWork(Delivery delivery)
        {
            var totalWork = 0;
            var remainingWork = 0;

            foreach (var feature in delivery.Features)
            {
                foreach (var featureWork in feature.FeatureWork)
                {
                    totalWork += featureWork.TotalWorkItems;
                    remainingWork += featureWork.RemainingWorkItems;
                }
            }

            var progress = totalWork == 0 ? 0.0 : Math.Round((double)(totalWork - remainingWork) / totalWork * 100.0, 2);

            return (progress, remainingWork, totalWork);
        }

        private static List<FeatureLikelihoodDto> CalculateFeatureLikelihoods(Delivery delivery, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var featureLikelihoods = new List<FeatureLikelihoodDto>();

            foreach (var feature in delivery.Features)
            {
                var likelihood = feature.GetLikelhoodForDate(delivery.Date, blackoutPeriods);

                var completionDates = feature.Forecast.CreateForecastDtos(blackoutPeriods, 70, 85, 95);

                featureLikelihoods.Add(new FeatureLikelihoodDto
                {
                    FeatureId = feature.Id,
                    LikelihoodPercentage = likelihood,
                    CompletionDates = completionDates.ToList(),
                    HasSufficientData = feature.Forecast.HasSufficientData,
                });
            }

            return featureLikelihoods;
        }
    }
}