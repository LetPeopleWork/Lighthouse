using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class ProjectDto
    {
        public ProjectDto(Project project)
        {
            Name = project.Name;
            Id = project.Id;
            LastUpdated = project.ProjectUpdateTime;

            foreach (var team in project.InvolvedTeams)
            {
                InvolvedTeams.Add(new TeamDto(team));
            }

            foreach (var feature in project.Features)
            {
                Features.Add(new FeatureDto(feature));
            }

            foreach (var milestone in project.Milestones)
            {
                Milestones.Add(new MilestoneDto(milestone));
            }
        }

        public string Name { get; }

        public int Id { get; }

        public List<FeatureDto> Features { get; } = new List<FeatureDto>();

        public List<TeamDto> InvolvedTeams { get; } = new List<TeamDto>();

        public List<MilestoneDto> Milestones { get; } = new List<MilestoneDto>();

        public DateTime LastUpdated { get; }
    }
}
