using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.API.DTO
{
    public class FeatureDto : WorkItemDto
    {
        /// <param name="asOf">
        /// D16 as extended by UPSTREAM-2. Without threading asOf through to the WorkItemDto base, the
        /// portfolio aging chart would stay today-anchored while the portfolio Work Item Age
        /// Percentiles card moved to as-of-endDate — the two surfaces disagreeing for the same range,
        /// which US-04 AC3 and CI2 both forbid.
        /// </param>
        #pragma warning disable S107 // Bug #5567 adds `today` as a VALUE, not a collaborator: WorkItemBase.WorkItemAge stopped reading the ambient clock, so the calendar day has to travel with the item. Grouping these optional projection flags into a record would obscure which ones the aging surfaces depend on.
        public FeatureDto(Feature feature, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods, bool isBlocked, DateTime? blockedSince, ISet<int>? readablePortfolioIds = null, IReadOnlyList<NamedCycleTimeValue>? namedCycleTimes = null, DateTime? asOf = null, StateAsOf? stateAsOf = null)
#pragma warning restore S107
            : base(feature, today, isBlocked, namedCycleTimes ?? [], blockedSince, asOf, stateAsOf)
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
    }
}
