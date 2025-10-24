using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.API.DTO
{
    public class ProjectDto : WorkTrackingSystemOptionsOwnerDtoBase
    {
        public ProjectDto() : base()
        {
        }

        public ProjectDto(Project project) : base(project)
        {
            InvolvedTeams.AddRange(project.CreateInvolvedTeamDtos());

            var forecasts = new List<WhenForecast>();

            foreach (var feature in project.Features.OrderBy(f => f, new FeatureComparer()))
            {
                Features.Add(new EntityReferenceDto(feature.Id, feature.Name));

                TotalWorkItems += feature.FeatureWork.Sum(fw => fw.TotalWorkItems);
                RemainingWorkItems += feature.FeatureWork.Sum(fw => fw.RemainingWorkItems);
                forecasts.Add(feature.Forecast);
            }

            foreach (var milestone in project.Milestones)
            {
                Milestones.Add(new MilestoneDto(milestone));
            }

            Forecasts = GetLatestForecasts(forecasts);
        }

        public List<EntityReferenceDto> Features { get; } = new List<EntityReferenceDto>();

        public List<EntityReferenceDto> InvolvedTeams { get; } = new List<EntityReferenceDto>();

        public List<MilestoneDto> Milestones { get; } = new List<MilestoneDto>();

        public List<WhenForecastDto> Forecasts { get; }

        public int TotalWorkItems { get; } = 0;

        public int RemainingWorkItems { get; } = 0;

        private List<WhenForecastDto> GetLatestForecasts(IEnumerable<WhenForecast> forecasts)
        {
            var whenForecasts = new List<WhenForecastDto>();

            if (!forecasts.Any())
            {
                return whenForecasts;
            }

            var latestForecast = forecasts.First();
            var latestForecastDays = latestForecast.GetProbability(85);

            foreach (var forecast in forecasts.Skip(1))
            {
                var forecastedDays = forecast.GetProbability(85);
                if (forecastedDays > latestForecastDays)
                {
                    latestForecastDays = forecastedDays;
                    latestForecast = forecast;
                }
            }

            whenForecasts.Add(new WhenForecastDto(latestForecast, 50));
            whenForecasts.Add(new WhenForecastDto(latestForecast, 70));
            whenForecasts.Add(new WhenForecastDto(latestForecast, 85));
            whenForecasts.Add(new WhenForecastDto(latestForecast, 95));

            return whenForecasts;
        }
    }
}
