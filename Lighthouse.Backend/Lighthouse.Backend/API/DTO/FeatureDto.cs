using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;

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
        public FeatureDto(Feature feature, IReadOnlyList<BlackoutPeriod> blackoutPeriods, ISet<int>? readablePortfolioIds = null, IReadOnlyList<NamedCycleTimeValue>? namedCycleTimes = null, DateTime? asOf = null)
            : base(feature, FeatureIsBlocked(feature), namedCycleTimes ?? [], null, asOf)
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

        private static readonly JsonSerializerOptions RuleSetJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly RuleEvaluator<Feature> FeatureRuleEvaluator = new();

        private static readonly FeatureFieldProvider FeatureFieldProvider = new();

        // Feature blocked status preserves the prior model-level semantics. NOTE: the FEATURE surface is out
        // of ADR-067 slice-01 scope (which routes the WORK-ITEM read path through IBlockedItemService). Routing
        // this feature-level derivation through a DI-resolved IBlockedItemService.IsBlocked(feature, portfolio)
        // is a bounded follow-up spanning the FeatureDto build sites in the portfolio/delivery controllers
        // (FeatureDto is constructed with `new`, not resolved via DI, so it mirrors BlockedItemService's own
        // stateless-construction pattern here rather than threading the port through four controllers).
        private static bool FeatureIsBlocked(Feature feature)
            => feature.Portfolios.Any(portfolio => IsBlockedByPortfolioRuleSet(feature, portfolio));

        private static bool IsBlockedByPortfolioRuleSet(Feature feature, Portfolio portfolio)
        {
            if (string.IsNullOrWhiteSpace(portfolio.BlockedRuleSetJson))
            {
                return false;
            }

            var ruleSet = JsonSerializer.Deserialize<WorkItemRuleSet>(portfolio.BlockedRuleSetJson, RuleSetJsonOptions);
            if (ruleSet == null || ruleSet.Conditions.Count == 0)
            {
                return false;
            }

            return FeatureRuleEvaluator.Match(ruleSet, [feature], FeatureFieldProvider).Any();
        }
    }
}
