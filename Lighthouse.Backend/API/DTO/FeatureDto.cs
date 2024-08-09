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

            foreach (var work in feature.FeatureWork)
            {
                if (!RemainingWork.ContainsKey(work.TeamId))
                {
                    RemainingWork.Add(work.TeamId, 0);
                    TotalWork.Add(work.TeamId, 0);
                }

                RemainingWork[work.TeamId] += work.RemainingWorkItems;
                TotalWork[work.TeamId] += work.TotalWorkItems;
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

        public Dictionary<int, int> TotalWork { get; } = new Dictionary<int, int>();

        public Dictionary<int, double> MilestoneLikelihood { get; } = new Dictionary<int, double>();

        public List<WhenForecastDto> Forecasts { get; } = new List<WhenForecastDto>();
    }
}
