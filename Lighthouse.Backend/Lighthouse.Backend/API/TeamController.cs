using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/teams/{teamId:int}")]
    [Route("api/latest/teams/{teamId:int}")]
    [ApiController]
    public class TeamController(
        IRepository<Team> teamRepository,
        IRepository<Portfolio> projectRepository,
        IWorkItemRepository workItemRepository,
        ITeamUpdater teamUpdateService,
        IPortfolioUpdater portfolioUpdater,
        IRepository<BlackoutPeriod> blackoutPeriodRepository,
        IRefreshLogService refreshLogService,
        IRbacAdministrationService rbacAdministrationService,
        IForecastFilterRuleService forecastFilterRuleService)
        : ControllerBase
    {
        internal const int MinStalenessThresholdDays = 0;
        internal const int MaxStalenessThresholdDays = 365;

        internal static bool IsStalenessThresholdInRange(int stalenessThresholdDays)
        {
            return stalenessThresholdDays is >= MinStalenessThresholdDays and <= MaxStalenessThresholdDays;
        }

        [HttpGet]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
        public async Task<ActionResult<TeamDto>> GetTeam(int teamId)
        {
            return await this.GetEntityByIdAnExecuteAction(teamRepository, teamId, async team =>
            {
                var allPortfolios = projectRepository.GetAll().ToList();
                var portfolioIds = allPortfolios.Select(p => p.Id).ToArray();
                var readablePortfolioIds = await rbacAdministrationService
                    .GetReadablePortfolioIdsAsync(User, portfolioIds, HttpContext?.RequestAborted ?? default)
                    .ConfigureAwait(false);
                var readablePortfolioIdSet = (readablePortfolioIds ?? portfolioIds)
                    .ToHashSet();

                var teamDto = team.CreateTeamDto(allPortfolios, readablePortfolioIdSet);
                var blackoutPeriods = blackoutPeriodRepository.GetAll().ToList();
                var throughputSettings = team.GetThroughputSettings();
                teamDto.HasThroughputBlackoutOverlap = blackoutPeriods.HasOverlapWithDateRange(throughputSettings.StartDate, throughputSettings.EndDate);
                teamDto.HasForecastFilter = forecastFilterRuleService.GetEffectiveRuleSet(team) != null;

                return teamDto;
            });
        }

        [HttpPost]
        [LicenseGuard(CheckTeamConstraint = true)]
        [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
        public ActionResult UpdateTeamData(int teamId)
        {
            var team = teamRepository.GetById(teamId);

            if (team == null)
            {
                return NotFound(null);
            }

            teamUpdateService.TriggerUpdate(team.Id);

            return Ok();
        }

        [HttpDelete]
        [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
        public async Task<IActionResult> DeleteTeam(int teamId)
        {
            var team = teamRepository.GetById(teamId);
            var affectedPortfolioIds = team?.Portfolios.Select(p => p.Id).ToList() ?? [];

            var owningPortfolioIds = projectRepository.GetAll()
                .Where(p => p.OwningTeamId == teamId)
                .Select(p => p.Id)
                .ToList();

            var allAffectedIds = affectedPortfolioIds.Union(owningPortfolioIds).Distinct().ToList();

            teamRepository.Remove(teamId);
            await teamRepository.Save();

            await refreshLogService.RemoveRefreshLogsForEntity(RefreshType.Team, teamId);

            foreach (var portfolioId in allAffectedIds)
            {
                portfolioUpdater.TriggerUpdate(portfolioId);
            }

            return NoContent();
        }

        [HttpPut]
        [LicenseGuard(CheckTeamConstraint = true)]
        [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
        public async Task<ActionResult<TeamSettingDto>> UpdateTeam(int teamId, TeamSettingDto teamSetting)
        {
            var baselineValidation = BaselineValidationService.Validate(
                teamSetting.ProcessBehaviourChartBaselineStartDate,
                teamSetting.ProcessBehaviourChartBaselineEndDate,
                teamSetting.DoneItemsCutoffDays);

            if (!baselineValidation.IsValid)
            {
                return BadRequest(baselineValidation.ErrorMessage);
            }

            var stateMappingValidation = StateMappingValidator.ValidateSettings(teamSetting);
            if (!stateMappingValidation.IsValid)
            {
                return BadRequest(stateMappingValidation.Errors);
            }

            if (!IsStalenessThresholdInRange(teamSetting.StalenessThresholdDays))
            {
                return BadRequest($"Staleness threshold must be between {MinStalenessThresholdDays} and {MaxStalenessThresholdDays} days.");
            }

            var teamForValidation = teamRepository.GetById(teamId);
            if (teamForValidation != null)
            {
                var filterValidation = ValidateForecastFilterRuleSet(teamSetting.ForecastFilterRuleSetJson, teamForValidation);
                if (!filterValidation.IsValid)
                {
                    return BadRequest(filterValidation.ErrorMessage);
                }
            }

            return await this.GetEntityByIdAnExecuteAction(teamRepository, teamId, async team =>
            {
                if (team.WorkItemRelatedSettingsChanged(teamSetting))
                {
                    workItemRepository.RemoveWorkItemsForTeam(team.Id);
                    await workItemRepository.Save();
                }

                team.SyncTeamWithTeamSettings(teamSetting);

                teamRepository.Update(team);
                await teamRepository.Save();

                var teamSettingDto = new TeamSettingDto(team);
                return teamSettingDto;
            });
        }

        [HttpGet("settings")]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
        public ActionResult<TeamSettingDto> GetTeamSettings(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team => new TeamSettingDto(team));
        }

        [HttpGet("forecast-filter/schema")]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
        public ActionResult<WorkItemRuleSchema> GetForecastFilterSchema(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team => forecastFilterRuleService.GetSchema(team));
        }

        private ForecastFilterValidationResult ValidateForecastFilterRuleSet(string? ruleSetJson, Team team)
        {
            if (string.IsNullOrWhiteSpace(ruleSetJson))
            {
                return ForecastFilterValidationResult.Valid();
            }

            WorkItemRuleSet? ruleSet;
            try
            {
                ruleSet = JsonSerializer.Deserialize<WorkItemRuleSet>(ruleSetJson);
            }
            catch (JsonException)
            {
                return ForecastFilterValidationResult.Invalid("Forecast filter rule set is not valid JSON.");
            }

            if (ruleSet == null || ruleSet.Conditions.Count == 0)
            {
                return ForecastFilterValidationResult.Valid();
            }

            if (!forecastFilterRuleService.ValidateRuleSet(ruleSet, team))
            {
                return ForecastFilterValidationResult.Invalid("Forecast filter rule set is invalid: unknown field key, unsupported operator, value exceeds maximum length, or rule count exceeds the allowed maximum.");
            }

            return ForecastFilterValidationResult.Valid();
        }

        private sealed record ForecastFilterValidationResult(bool IsValid, string? ErrorMessage)
        {
            public static ForecastFilterValidationResult Valid() => new(true, null);

            public static ForecastFilterValidationResult Invalid(string message) => new(false, message);
        }
    }
}
