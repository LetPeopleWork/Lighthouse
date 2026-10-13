using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// How big the Features a Team works on are, measured by the Team's own part of each one rather
    /// than by every Team's work under it.
    /// </summary>
    [Route("api/v1/teams/{teamId:int}/metrics")]
    [Route("api/latest/teams/{teamId:int}/metrics")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
    public class TeamFeatureSizeMetricsController(
        IRepository<Team> teamRepository,
        ITeamMetricsService teamMetricsService,
        IBlackoutPeriodService blackoutPeriodService,
        IBlockedItemService blockedItemService,
        IRbacAdministrationService rbacAdministrationService,
        ILighthouseClock clock) : ControllerBase
    {
        private const string StartDateMustBeBeforeEndDateErrorMessage = "Start date must be before end date.";

        [HttpGet("allFeaturesForSizeChart")]
        public async Task<ActionResult<IEnumerable<FeatureDto>>> GetAllFeaturesForSizeChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return await this.GetEntityByIdAnExecuteAction(teamRepository, teamId, async team =>
            {
                var features = teamMetricsService.GetAllFeaturesForSizeChart(team, startDate, endDate).ToList();
                var readablePortfolioIds = await GetReadablePortfolioIds(features);
                var forecastWindowStart = clock.TodayAsUtcMidnight;
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                    forecastWindowStart, FeatureForecastWindow.EndFor(forecastWindowStart, features));

                return features.Select(f => new FeatureDto(f, clock, blackoutPeriods, f.Portfolios.Any(p => blockedItemService.IsBlocked(f, p)), null, readablePortfolioIds)
                {
                    Size = f.SizeFor(team.Id),
                });
            });
        }

        [HttpGet("sizePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetSizePercentiles(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
                teamMetricsService.GetSizePercentilesForTeam(team, startDate, endDate));
        }

        [HttpGet("featureSizePercentilesInfo")]
        public ActionResult<FeatureSizePercentilesInfoDto> GetFeatureSizePercentilesInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
                teamMetricsService.GetFeatureSizePercentilesInfoForTeam(team, startDate, endDate));
        }

        // A reader of the Team may see a Feature from a Portfolio they cannot open; the Feature still
        // counts, but that Portfolio's name is withheld from them.
        private async Task<HashSet<int>> GetReadablePortfolioIds(List<Feature> features)
        {
            var requestedPortfolioIds = features.SelectMany(f => f.Portfolios).Select(p => p.Id).Distinct().ToArray();
            var readablePortfolioIds = await rbacAdministrationService
                .GetReadablePortfolioIdsAsync(User, requestedPortfolioIds, HttpContext.RequestAborted)
                .ConfigureAwait(false);

            return readablePortfolioIds is { } ? readablePortfolioIds.ToHashSet() : requestedPortfolioIds.ToHashSet();
        }
    }
}
