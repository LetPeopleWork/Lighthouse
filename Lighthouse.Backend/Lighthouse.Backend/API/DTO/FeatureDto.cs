using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureDto : WorkItemDto
    {
        public FeatureDto(Feature feature, IReadOnlyList<BlackoutPeriod> blackoutPeriods, ISet<int>? readablePortfolioIds = null, IReadOnlyList<NamedCycleTimeValue>? namedCycleTimes = null)
            : base(feature, FeatureIsBlocked(feature), namedCycleTimes ?? [])
        {
            LastUpdated = DateTime.SpecifyKind(feature.Forecast?.CreationTime ?? DateTime.MinValue, DateTimeKind.Utc);
            IsUsingDefaultFeatureSize = feature.IsUsingDefaultFeatureSize;
            Size = feature.Size;
            OwningTeam = feature.OwningTeam;

            Forecasts.AddRange(feature.Forecast?.CreateForecastDtos(blackoutPeriods, 50, 70, 85, 95) ?? []);

            foreach (var work in feature.FeatureWork)
            {
                if (RemainingWork.TryAdd(work.TeamId, 0))
                {
                    TotalWork.Add(work.TeamId, 0);
                }

                RemainingWork[work.TeamId] += work.RemainingWorkItems;
                TotalWork[work.TeamId] += work.TotalWorkItems;
            }

            foreach (var project in feature.Portfolios)
            {
                if (readablePortfolioIds is not null && !readablePortfolioIds.Contains(project.Id))
                {
                    continue;
                }

                Projects.Add(new EntityReferenceDto(project.Id, project.Name));
            }
        }
        
        public bool IsUsingDefaultFeatureSize { get; }

        public int Size { get; }

        public string OwningTeam { get; }

        public List<EntityReferenceDto> Projects { get; } = new List<EntityReferenceDto>();

        public DateTime LastUpdated { get; }

        public Dictionary<int, int> RemainingWork { get; } = new Dictionary<int, int>();

        public Dictionary<int, int> TotalWork { get; } = new Dictionary<int, int>();

        public List<WhenForecastDto> Forecasts { get; } = new List<WhenForecastDto>();

        // Feature blocked status preserves the prior model-level semantics. NOTE: the FEATURE surface is out
        // of ADR-067 slice-01 scope (which routes the WORK-ITEM read path through IBlockedItemService). Routing
        // this feature-level derivation through IBlockedItemService.IsBlocked(feature, portfolio) is a bounded
        // follow-up spanning the FeatureDto build sites in the portfolio/delivery controllers.
        private static bool FeatureIsBlocked(Feature feature)
            => feature.Portfolios.Any(portfolio =>
                portfolio.BlockedStates.Contains(feature.State) || portfolio.BlockedTags.Any(feature.Tags.Contains));
    }
}
