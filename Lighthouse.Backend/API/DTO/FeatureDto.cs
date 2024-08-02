using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureDto
    {
        public FeatureDto(Feature feature)
        {
            Name = feature.Name;
            Id = feature.Id;
            Url = feature.Url;
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

            foreach (var project in feature.Projects)
            {
                Projects.Add(project.Id, project.Name);

                foreach (var milestone in project.Milestones)
                {
                    var likelihood = feature.GetLikelhoodForDate(milestone.Date);
                    MilestoneLikelihood.Add(milestone.Id, likelihood);
                }
            }
        }

        public string Name { get; set; }

        public int Id { get; set; }

        public string Url { get; set; }

        public Dictionary<int, string> Projects { get; } = new Dictionary<int, string>();

        public DateTime LastUpdated { get; }

        public Dictionary<int, int> RemainingWork { get; } = new Dictionary<int, int>();

        public Dictionary<int, double> MilestoneLikelihood { get; } = new Dictionary<int, double>();

        public List<WhenForecastDto> Forecasts { get; } = new List<WhenForecastDto>();
    }
}
