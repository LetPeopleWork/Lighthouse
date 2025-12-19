using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureLikelihoodDto
    {
        public int FeatureId { get; set; }
        public double LikelihoodPercentage { get; set; }
    }

    public class DeliveryWithLikelihoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int PortfolioId { get; set; }
        public double LikelihoodPercentage { get; set; }
        public double Progress { get; set; }
        public int RemainingWork { get; set; }
        public int TotalWork { get; set; }
        public List<int> Features { get; set; } = new List<int>();
        public List<FeatureLikelihoodDto> FeatureLikelihoods { get; set; } = new List<FeatureLikelihoodDto>();

        public static DeliveryWithLikelihoodDto FromDelivery(Delivery delivery)
        {
            var featureLikelihoods = CalculateFeatureLikelihoods(delivery);
            var likelihoodPercentage = GetMinimumLikelihood(featureLikelihoods);
            var (progress, remainingWork, totalWork) = CalculateDeliveryWork(delivery);

            return new DeliveryWithLikelihoodDto
            {
                Id = delivery.Id,
                Name = delivery.Name,
                Date = delivery.Date,
                PortfolioId = delivery.PortfolioId,
                LikelihoodPercentage = likelihoodPercentage,
                Progress = progress,
                RemainingWork = remainingWork,
                TotalWork = totalWork,
                Features = delivery.Features.Select(f => f.Id).ToList(),
                FeatureLikelihoods = featureLikelihoods
            };
        }

        private static double GetMinimumLikelihood(List<FeatureLikelihoodDto> featureLikelihoods)
        {
            var likelihoods = featureLikelihoods
                .Where(fl => fl.LikelihoodPercentage > 0)
                .Select(fl => fl.LikelihoodPercentage)
                .ToList();

            // Return 0 if no features have forecasts
            if (likelihoods.Count == 0)
            {
                return 0.0;
            }

            // Return the minimum likelihood (most conservative estimate)
            return likelihoods.Min();
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
                var likelihood = 0.0;
                
                if (featureForecast != null)
                {
                    likelihood = featureForecast.GetLikelihood(daysToTarget);
                }

                featureLikelihoods.Add(new FeatureLikelihoodDto
                {
                    FeatureId = feature.Id,
                    LikelihoodPercentage = likelihood
                });
            }

            return featureLikelihoods;
        }
    }
}