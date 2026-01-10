using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureLikelihoodDto
    {
        public int FeatureId { get; init; }
        
        public double LikelihoodPercentage { get; init; }
        
        public List<WhenForecastDto> CompletionDates { get; init; } = [];
    }

    public class DeliveryWithLikelihoodDto
    {
        public int Id { get; private init; }
        
        public string Name { get; private init; } = string.Empty;
        
        public DateTime Date { get; private init; }
        
        public int PortfolioId { get; private init; }
        
        public double LikelihoodPercentage { get; private init; }
        
        public List<WhenForecastDto> CompletionDates { get; private init; } = [];
        
        public double Progress { get; private init; }
        
        public int RemainingWork { get; private init; }
        
        public int TotalWork { get; private init; }
        
        public List<int> Features { get; private init; } = [];
        
        public List<FeatureLikelihoodDto> FeatureLikelihoods { get; private init; } = [];

        public static DeliveryWithLikelihoodDto FromDelivery(Delivery delivery)
        {
            var featureLikelihoods = CalculateFeatureLikelihoods(delivery);

            var likelihoodPercentage = 0.0;
            var completionDates = new List<WhenForecastDto>();
            
            var leastLikelyFeature = GetLeastLikelyFeature(featureLikelihoods);

            if (leastLikelyFeature != null)
            {
                likelihoodPercentage = leastLikelyFeature.LikelihoodPercentage;
                completionDates.AddRange(leastLikelyFeature.CompletionDates);
            }
            
            var (progress, remainingWork, totalWork) = CalculateDeliveryWork(delivery);

            return new DeliveryWithLikelihoodDto
            {
                Id = delivery.Id,
                Name = delivery.Name,
                Date = delivery.Date,
                PortfolioId = delivery.PortfolioId,
                LikelihoodPercentage = likelihoodPercentage,
                CompletionDates = completionDates,
                Progress = progress,
                RemainingWork = remainingWork,
                TotalWork = totalWork,
                Features = delivery.Features.Select(f => f.Id).ToList(),
                FeatureLikelihoods = featureLikelihoods
            };
        }

        private static FeatureLikelihoodDto? GetLeastLikelyFeature(List<FeatureLikelihoodDto> featureLikelihoods)
        {
            var likelihoods = featureLikelihoods
                .Where(fl => fl.LikelihoodPercentage > 0)
                .OrderByDescending(fl => fl.LikelihoodPercentage)
                .ToList();

            // Return 0 if no features have forecasts
            if (likelihoods.Count == 0)
            {
                return null;
            }

            // Return the minimum likelihood (most conservative estimate)
            return likelihoods.Last();
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

        private static List<FeatureLikelihoodDto> CalculateFeatureLikelihoods(Delivery delivery)
        {
            var daysToTarget = (int)(delivery.Date - DateTime.UtcNow).TotalDays;
            var featureLikelihoods = new List<FeatureLikelihoodDto>();

            foreach (var feature in delivery.Features)
            {
                var featureForecast = feature.Forecast;
                var likelihood = featureForecast.GetLikelihood(daysToTarget);

                var completionDates = feature.Forecast.CreateForecastDtos(70, 85, 95);

                featureLikelihoods.Add(new FeatureLikelihoodDto
                {
                    FeatureId = feature.Id,
                    LikelihoodPercentage = likelihood,
                    CompletionDates = completionDates.ToList(),
                });
            }

            return featureLikelihoods;
        }
    }
}