using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.API.DTO
{
    public static class DtoExtensions
    {
        public static IEnumerable<WhenForecastDto> CreateForecastDtos(this WhenForecast whenForecast, params int[] probabilities)
        {
            var forecastDtos = new List<WhenForecastDto>();
            foreach (var probability in probabilities)
            {
                forecastDtos.Add(
                        new WhenForecastDto(whenForecast, probability)
                    );
            }

            return forecastDtos;
        }
        public static IEnumerable<HowManyForecastDto> CreateForecastDtos(this HowManyForecast howManyForecast, params int[] probabilities)
        {
            var forecastDtos = new List<HowManyForecastDto>();
            foreach (var probability in probabilities)
            {
                forecastDtos.Add(
                        new HowManyForecastDto { Probability = probability, ExpectedItems = howManyForecast.GetProbability(probability) }
                    );
            }

            return forecastDtos;
        }
    }
}
