using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class PortfolioDto : WorkTrackingSystemOptionsOwnerDtoBase
    {
        public PortfolioDto() : base()
        {
        }

        public PortfolioDto(Portfolio portfolio) : base(portfolio)
        {
            InvolvedTeams.AddRange(portfolio.CreateInvolvedTeamDtos());

            foreach (var feature in portfolio.Features.OrderBy(f => f, new FeatureComparer()))
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
