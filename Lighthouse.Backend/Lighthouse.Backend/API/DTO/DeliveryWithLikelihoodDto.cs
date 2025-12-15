using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class DeliveryWithLikelihoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public double LikelihoodPercentage { get; set; }

        public static DeliveryWithLikelihoodDto FromDelivery(Delivery delivery)
        {
            var likelihoodPercentage = CalculateDeliveryLikelihood(delivery);

            return new DeliveryWithLikelihoodDto
            {
                Id = delivery.Id,
                Name = delivery.Name,
                Date = delivery.Date,
                LikelihoodPercentage = likelihoodPercentage
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
    }
}