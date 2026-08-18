using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;

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
#pragma warning disable S107 // Bug #5567: the instance calendar has to arrive with the item, taking this one past the threshold. Grouping the projection flags into a record would hide which ones the aging surfaces depend on.
        public FeatureDto(Feature feature, ILighthouseClock clock, IReadOnlyList<BlackoutPeriod> blackoutPeriods, bool isBlocked, DateTime? blockedSince, ISet<int>? readablePortfolioIds = null, IReadOnlyList<NamedCycleTimeValue>? namedCycleTimes = null, DateTime? asOf = null, StateAsOf? stateAsOf = null)
#pragma warning restore S107
            : base(feature, clock, isBlocked, namedCycleTimes ?? [], blockedSince, asOf, stateAsOf)
        {
            LastUpdated = DateTime.SpecifyKind(feature.Forecast?.CreationTime ?? DateTime.MinValue, DateTimeKind.Utc);
            IsUsingDefaultFeatureSize = feature.IsUsingDefaultFeatureSize;
            Size = feature.Size;
            OwningTeam = feature.OwningTeam;

            // A feature a contributing team cannot be forecast for reports no dates at all rather than
            // dates built from the teams that could be forecast (ADR-112).
            TeamsWithoutForecast.AddRange(feature.TeamsWithoutForecast.Select(team => team.Name).Distinct().Order());

            if (feature.CanBeForecast)
            {
                Forecasts.AddRange(feature.Forecast?.CreateForecastDtos(clock.Today, blackoutPeriods, 50, 70, 85, 95) ?? []);
            }

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

        // Non-empty means the feature cannot be forecast, and names the teams to chase (ADR-112).
        public List<string> TeamsWithoutForecast { get; } = [];

        // The place this Feature holds across the whole instance; null on read paths that do not number (ADR-135).
        public int? Position { get; set; }

        // The move verdict is decided here and rendered as given (ADR-136). Null on read paths that do not
        // authorize; a client must read "absent" as "not allowed", because absent is not permission.
        public bool? CanMove { get; set; }

        public string? MoveBlockReason { get; set; }

        // Only Portfolios the caller may read are ever named (ADR-136 §3).
        public List<EntityReferenceDto> BlockingPortfolios { get; } = [];

        // How many of the Features this Lighthouse holds the Feature is waiting on. A link naming
        // something not held is stored but does not count, so this is never the stored total. Null on
        // read paths that never match the links up, because absent means "not worked out here" rather
        // than "waiting on nothing".
        public int? DependsOnCount { get; set; }
    }
}
