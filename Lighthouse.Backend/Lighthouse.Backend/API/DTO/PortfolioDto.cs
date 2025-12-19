using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class PortfolioDto : WorkTrackingSystemOptionsOwnerDtoBase
    {
        public PortfolioDto() : base()
        {
        }

        public PortfolioDto(Portfolio project) : base(project)
        {
            InvolvedTeams.AddRange(project.CreateInvolvedTeamDtos());

            foreach (var feature in project.Features.OrderBy(f => f, new FeatureComparer()))
            {
                Features.Add(new EntityReferenceDto(feature.Id, feature.Name));
            }
        }

        public List<EntityReferenceDto> Features { get; } = [];

        public List<EntityReferenceDto> InvolvedTeams { get; } = [];
    }
}
