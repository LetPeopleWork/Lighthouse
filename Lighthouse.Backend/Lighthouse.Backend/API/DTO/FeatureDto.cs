using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureDto
    {
        public FeatureDto(Feature feature)
        {
            Name = feature.Name;
            Id = feature.Id;
            FeatureReference = feature.ReferenceId;
            Url = feature.Url;
            StateCategory = feature.StateCategory;
            Order = feature.Order;
            LastUpdated = feature.Forecast?.CreationTime ?? DateTime.MinValue;

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

        public string Name { get; }

        public int Id { get; }

        public string FeatureReference { get; }

        public string Url { get; }

        public StateCategories StateCategory { get; }

        public string Order { get; }
        
        public bool IsUsingDefaultFeatureSize { get; }

        public Dictionary<int, string> Projects { get; } = new Dictionary<int, string>();

        public DateTime LastUpdated { get; }

        public Dictionary<int, int> RemainingWork { get; } = new Dictionary<int, int>();

        public Dictionary<int, int> TotalWork { get; } = new Dictionary<int, int>();

        public Dictionary<int, double> MilestoneLikelihood { get; } = new Dictionary<int, double>();

        public List<WhenForecastDto> Forecasts { get; } = new List<WhenForecastDto>();
    }
}
