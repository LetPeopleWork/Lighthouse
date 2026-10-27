using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.WorkItems;

namespace Lighthouse.Backend.API
{
    /// <summary>Turns what a list read worked out into the answer a reader is handed, each row carrying what was decided about it.</summary>
    public class FeatureListRows(
        IBlackoutPeriodService blackoutPeriodService,
        IBlockedItemService blockedItemService,
        ILighthouseClock clock)
    {
        public FeatureListDto Of(FeatureListRead read)
        {
            var list = new FeatureListDto();

            list.Features.AddRange(Rows(read));
            list.Parents.AddRange(read.Parents.Select(parent => new FeatureListParentDto(parent.ReferenceId, parent.Name, parent.Url)));
            list.ActiveWork.AddRange(read.ActiveWork.Select(work => new FeatureListActiveWorkDto(work.TeamId, work.FeatureReferenceIds)));

            return list;
        }

        private List<FeatureListRowDto> Rows(FeatureListRead read)
        {
            var forecastWindowStart = clock.TodayAsUtcMidnight;
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                forecastWindowStart, FeatureForecastWindow.EndFor(forecastWindowStart, read.Features));

            return [.. read.Features.Select(feature => ARow(feature, blackoutPeriods, read))];
        }

        private FeatureListRowDto ARow(Feature feature, IReadOnlyList<BlackoutPeriod> blackoutPeriods, FeatureListRead read)
        {
            var isBlocked = feature.Portfolios.Any(portfolio => blockedItemService.IsBlocked(feature, portfolio));
            var verdict = read.Verdicts[feature.Id];

            var row = new FeatureListRowDto(feature, clock, blackoutPeriods, isBlocked, read.ReadablePortfolioIds)
            {
                // Null only if the Feature was deleted between the row read and the position read.
                Position = read.Positions.TryGetValue(feature.Id, out var position) ? position : null,
                CanMove = verdict.CanMove,
                MoveBlockReason = verdict.BlockReason,
            };

            row.DependsOn.AddRange(read.Dependencies.Of(feature));
            row.BlockingPortfolios.AddRange(verdict.BlockingPortfolios.Select(portfolio => new EntityReferenceDto(portfolio.Id, portfolio.Name)));

            return row;
        }
    }
}
