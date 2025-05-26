using Lighthouse.Backend.Models;

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

        public List<EntityReferenceDto> InvolvedTeams { get; } = new List<EntityReferenceDto>();

        public List<MilestoneDto> Milestones { get; } = new List<MilestoneDto>();
    }
}
