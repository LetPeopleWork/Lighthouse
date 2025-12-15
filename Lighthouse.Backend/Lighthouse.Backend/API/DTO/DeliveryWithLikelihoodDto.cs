using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
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

        public static DeliveryWithLikelihoodDto FromDelivery(Delivery delivery)
        {
            var likelihoodPercentage = CalculateDeliveryLikelihood(delivery);
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
                Features = delivery.Features.Select(f => f.Id).ToList()
            };
        }

        private static double CalculateDeliveryLikelihood(Delivery delivery)
        {
            var daysToTarget = (int)(delivery.Date - DateTime.UtcNow).TotalDays;
            
            var featureLikelihoods = new List<double>();

            foreach (var feature in delivery.Features)
            {
                var featureForecast = feature.Forecast;
                if (featureForecast != null && featureForecast.TotalTrials > 0)
                {
                    var likelihood = featureForecast.GetLikelihood(daysToTarget);
                    featureLikelihoods.Add(likelihood);
                }
            }

            // Return 0 if no features have forecasts
            if (featureLikelihoods.Count == 0)
            {
                return 0.0;
            }

            // Return the minimum likelihood (most conservative estimate)
            return featureLikelihoods.Min();
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
    }
}