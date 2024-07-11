using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureDto
    {
        public FeatureDto(Feature feature)
        {
            Name = feature.Name;
            Id = feature.Id;
            LastUpdated = feature.Forecast?.CreationTime ?? DateTime.MinValue;

            Forecasts.AddRange(GetForecastDtosForFeature(feature));

            foreach (var remainingWork in feature.RemainingWork)
            {
                if (!RemainingWork.ContainsKey(remainingWork.TeamId))
                {
                    RemainingWork.Add(remainingWork.TeamId, 0);
                }

                RemainingWork[remainingWork.TeamId] += remainingWork.RemainingWorkItems;
            }
        }

        public string Name { get; set; }

        public int Id { get; set; }

        public DateTime LastUpdated { get; }

        public Dictionary<int, int> RemainingWork { get; } = new Dictionary<int, int>();

        public List<WhenForecastDto> Forecasts { get; } = new List<WhenForecastDto>();

        private IEnumerable<WhenForecastDto> GetForecastDtosForFeature(Feature feature)
        {
            if (feature.Forecast == null)
            {
                return Enumerable.Empty<WhenForecastDto>();
            }

            var forecasts = new List<WhenForecastDto>
            {
                new WhenForecastDto { Probability = 50, ExpectedDate = GetFutureDate(feature.Forecast.GetProbability(50)) },
                new WhenForecastDto { Probability = 70, ExpectedDate = GetFutureDate(feature.Forecast.GetProbability(70)) },
                new WhenForecastDto { Probability = 85, ExpectedDate = GetFutureDate(feature.Forecast.GetProbability(85)) },
                new WhenForecastDto { Probability = 95, ExpectedDate = GetFutureDate(feature.Forecast.GetProbability(95)) },
            };

            return forecasts;
        }

        private DateTime GetFutureDate(int daysInFuture)
        {
            return DateTime.Today.AddDays(daysInFuture);
        }
    }
}
