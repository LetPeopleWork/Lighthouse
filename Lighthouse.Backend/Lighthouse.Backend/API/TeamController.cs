using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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
        IBlackoutPeriodService blackoutPeriodService,
        IUpdateQueueService updateQueueService,
        IRbacAdministrationService rbacAdministrationService,
        IForecastFilterRuleService forecastFilterRuleService,
        IBlockedItemService blockedItemService,
        ILicenseService licenseService)
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
                var throughputSettings = team.GetThroughputSettings();
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(throughputSettings.StartDate, throughputSettings.EndDate);
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
        public async Task<IActionResult> DeleteTeam(int teamId, CancellationToken cancellationToken)
        {
            var team = teamRepository.GetById(teamId);
            var affectedPortfolioIds = team?.Portfolios.Select(p => p.Id).ToList() ?? [];

            var owningPortfolioIds = projectRepository.GetAll()
                .Where(p => p.OwningTeamId == teamId)
                .Select(p => p.Id)
                .ToList();

            IReadOnlyList<int> allAffectedIds = affectedPortfolioIds.Union(owningPortfolioIds).Distinct().ToList();

            await updateQueueService.EnqueueAndAwaitAsync(
                UpdateType.TeamDelete,
                teamId,
                async serviceProvider =>
                {
                    var teams = serviceProvider.GetRequiredService<IRepository<Team>>();
                    teams.Remove(teamId);
                    await teams.Save();

                    var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();
                    await dispatcher.PublishAsync(new TeamDeleted(teamId, allAffectedIds), cancellationToken);
                },
                cancellationToken);

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

                var blockedError = ValidateBlockedRuleSet(teamSetting.BlockedRuleSetJson, teamForValidation);
                if (blockedError != null)
                {
                    return BadRequest(blockedError);
                }
            }

            var canUsePremiumFeatures = licenseService.CanUsePremiumFeatures();
            if (canUsePremiumFeatures)
            {
                var cycleTimeValidation = CycleTimeDefinitionValidator.ValidateSettings(teamSetting);
                if (!cycleTimeValidation.IsValid)
                {
                    return BadRequest(cycleTimeValidation.Errors);
                }
            }

            return await this.GetEntityByIdAnExecuteAction(teamRepository, teamId, async team =>
            {
                if (!canUsePremiumFeatures)
                {
                    teamSetting.CycleTimeDefinitions = team.CycleTimeDefinitions
                        .Select(definition => new CycleTimeDefinitionDto(definition, true))
                        .ToList();
                }

                if (team.WorkItemRelatedSettingsChanged(teamSetting))
                {
                    workItemRepository.RemoveWorkItemsForTeam(team.Id);
                    await workItemRepository.Save();
                }

                team.SyncTeamWithTeamSettings(teamSetting);
                team.BlockedRuleSetJson = teamSetting.BlockedRuleSetJson;

                teamRepository.Update(team);

                if (teamSetting.ConcurrencyToken.HasValue)
                {
                    teamRepository.ApplyConcurrencyTokenForEdit(team, teamSetting.ConcurrencyToken.Value);
                }

                await teamRepository.Save();

                var teamSettingDto = new TeamSettingDto(team);
                return teamSettingDto;
            });
        }

        [HttpGet("settings")]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
        public ActionResult<TeamSettingDto> GetTeamSettings(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
            {
                var teamSettingDto = new TeamSettingDto(team)
                {
                    BlockedRuleSetJson = JsonSerializer.Serialize(blockedItemService.GetEffectiveRuleSet(team)),
                };
                return teamSettingDto;
            });
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

        private string? ValidateBlockedRuleSet(string? ruleSetJson, Team team)
        {
            if (string.IsNullOrWhiteSpace(ruleSetJson))
            {
                return null;
            }

            WorkItemRuleSet? ruleSet;
            try
            {
                ruleSet = JsonSerializer.Deserialize<WorkItemRuleSet>(ruleSetJson);
            }
            catch (JsonException)
            {
                return "Blocked rule set is not valid JSON.";
            }

            if (ruleSet == null || ruleSet.Conditions.Count == 0)
            {
                return null;
            }

            if (!blockedItemService.ValidateRuleSet(ruleSet, team))
            {
                return "Blocked rule set is invalid: unknown field key, unsupported operator, value exceeds maximum length, or rule count exceeds the allowed maximum.";
            }

            return null;
        }

        private sealed record ForecastFilterValidationResult(bool IsValid, string? ErrorMessage)
        {
            public static ForecastFilterValidationResult Valid() => new(true, null);

            public static ForecastFilterValidationResult Invalid(string message) => new(false, message);
        }
    }
}
