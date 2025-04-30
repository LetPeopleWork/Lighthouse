using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureDto : WorkItemDto
    {
        public FeatureDto(Feature feature) : base(feature)
        {            
            LastUpdated = DateTime.SpecifyKind(feature.Forecast?.CreationTime ?? DateTime.MinValue, DateTimeKind.Utc);
            IsUsingDefaultFeatureSize = feature.IsUsingDefaultFeatureSize;

            Forecasts.AddRange(feature.Forecast?.CreateForecastDtos(50, 70, 85, 95) ?? []);

            foreach (var work in feature.FeatureWork)
            {
                if (RemainingWork.TryAdd(work.TeamId, 0))
                {
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
        
        public bool IsUsingDefaultFeatureSize { get; }

        public Dictionary<int, string> Projects { get; } = new Dictionary<int, string>();

        public DateTime LastUpdated { get; }

        public Dictionary<int, int> RemainingWork { get; } = new Dictionary<int, int>();

        public Dictionary<int, int> TotalWork { get; } = new Dictionary<int, int>();

        public Dictionary<int, double> MilestoneLikelihood { get; } = new Dictionary<int, double>();

        public List<WhenForecastDto> Forecasts { get; } = new List<WhenForecastDto>();
    }
}
