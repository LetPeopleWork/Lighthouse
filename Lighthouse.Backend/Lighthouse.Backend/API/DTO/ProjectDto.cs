using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class ProjectDto : WorkTrackingSystemOptionsOwnerDtoBase
    {
        public ProjectDto(Project project) : base(project)
        {
            InvolvedTeams.AddRange(project.CreateInvolvedTeamDtos());

            foreach (var feature in project.Features.OrderBy(f => f, new FeatureComparer()))
            {
                Features.Add(new FeatureDto(feature));
            }

            foreach (var milestone in project.Milestones)
            {
                Milestones.Add(new MilestoneDto(milestone));
            }
        }

        public List<FeatureDto> Features { get; } = new List<FeatureDto>();

        public List<TeamDto> InvolvedTeams { get; } = new List<TeamDto>();

        public List<MilestoneDto> Milestones { get; } = new List<MilestoneDto>();
    }
}
