using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.API.DTO
{
    public class PortfolioDto : WorkTrackingSystemOptionsOwnerDtoBase
    {
        public PortfolioDto() : base()
        {
        }

        public PortfolioDto(Portfolio portfolio, IFeatureOrdering featureOrdering, ISet<int>? readableTeamIds = null) : base(portfolio)
        {
            InvolvedTeams.AddRange(portfolio.CreateInvolvedTeamDtos(readableTeamIds));

            foreach (var feature in featureOrdering.Order(portfolio.Features))
            {
                Features.Add(new EntityReferenceDto(feature.Id, feature.Name));
            }

            if (portfolio.UsePercentileToCalculateDefaultAmountOfWorkItems)
            {
                FeatureSizeTargetProbability = portfolio.DefaultWorkItemPercentile;
                FeatureSizeTargetRange = portfolio.PercentileHistoryInDays ?? 0;
            }
        }

        public List<EntityReferenceDto> Features { get; } = [];

        public List<EntityReferenceDto> InvolvedTeams { get; } = [];

        public int FeatureSizeTargetProbability { get; }

        public int FeatureSizeTargetRange { get; }
    }
}
