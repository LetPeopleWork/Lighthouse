using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class TeamDto
    {
        public TeamDto()
        {
        }

        public TeamDto(Team team)
        {
            Name = team.Name;
            Id = team.Id;
            FeatureWip = team.FeatureWIP;
            LastUpdated = team.TeamUpdateTime;
            FeaturesInProgress = team.FeaturesInProgress;
        }

        public string Name { get; set; }

        public int Id { get; set; }

        public int FeatureWip { get; set; }

        public List<string> FeaturesInProgress { get; } = new List<string>();

        public DateTime LastUpdated { get; set; }

        public List<FeatureDto> Features { get; } = new List<FeatureDto>();

        public List<ProjectDto> Projects { get; } = new List<ProjectDto>();
    }
}
