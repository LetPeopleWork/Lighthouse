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

            Forecasts.AddRange(feature.Forecast?.CreateForecastDtos([50, 70, 85, 95]) ?? Enumerable.Empty<WhenForecastDto>());

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
    }
}
