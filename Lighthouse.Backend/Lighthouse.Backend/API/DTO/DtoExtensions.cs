using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.API.DTO
{
    public static class DtoExtensions
    {
        public static IEnumerable<WhenForecastDto> CreateForecastDtos(this WhenForecast whenForecast, IReadOnlyList<BlackoutPeriod> blackoutPeriods, params int[] probabilities)
        {
            var forecastDtos = new List<WhenForecastDto>();
            foreach (var probability in probabilities)
            {
                forecastDtos.Add(
                        new WhenForecastDto(whenForecast, probability, blackoutPeriods)
                    );
            }

            return forecastDtos;
        }

        public static IEnumerable<ForecastDto> CreateForecastDtos(this HowManyForecast howManyForecast, params int[] probabilities)
        {
            var forecastDtos = new List<ForecastDto>();
            foreach (var probability in probabilities)
            {
                forecastDtos.Add(
                        new ForecastDto { Probability = probability, Value = howManyForecast.GetProbability(probability) }
                    );
            }

            return forecastDtos;
        }

        public static IEnumerable<EntityReferenceDto> CreateInvolvedTeamDtos(this Portfolio portfolio, ISet<int>? readableTeamIds = null)
        {
            foreach (var team in portfolio.Teams)
            {
                if (readableTeamIds is not null && !readableTeamIds.Contains(team.Id))
                {
                    continue;
                }

                yield return new EntityReferenceDto(team.Id, team.Name);
            }
        }
    }
}
