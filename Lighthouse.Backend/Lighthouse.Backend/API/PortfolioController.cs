using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/portfolios/{portfolioId:int}")]
    [Route("api/latest/portfolios/{portfolioId:int}")]
    [ApiController]
    public class PortfolioController(
        IRepository<Portfolio> portfolioRepository,
        IRepository<Team> teamRepository,
        IPortfolioUpdater portfolioUpdater,
        IRbacAdministrationService rbacAdministrationService,
        IBlockedItemService blockedItemService,
        IUpdateQueueService updateQueueService)
        : ControllerBase
    {
        [HttpGet]
        [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
        public async Task<ActionResult<PortfolioDto>> Get(int portfolioId)
        {
            return await this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, async portfolio =>
            {
                var teamIds = portfolio.Teams.Select(t => t.Id).ToArray();
                var readableTeamIds = await rbacAdministrationService
                    .GetReadableTeamIdsAsync(User, teamIds, HttpContext?.RequestAborted ?? default)
                    .ConfigureAwait(false);
                var readableTeamIdSet = (readableTeamIds ?? teamIds)
                    .ToHashSet();

                return new PortfolioDto(portfolio, readableTeamIdSet);
            });
        }

        [HttpPost("refresh")]
        [LicenseGuard(CheckPortfolioConstraint = true)]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        public ActionResult UpdateFeaturesForPortfolio(int portfolioId)
        {
            portfolioUpdater.TriggerUpdate(portfolioId);

            return Ok();
        }

        [HttpDelete]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        public async Task<IActionResult> DeletePortfolio(int portfolioId)
        {
            if (!portfolioRepository.Exists(portfolioId))
            {
                return NotFound();
            }

            await updateQueueService.EnqueueAndAwaitAsync(
                UpdateType.PortfolioDelete,
                portfolioId,
                async sp =>
                {
                    var portfolios = sp.GetRequiredService<IRepository<Portfolio>>();
                    portfolios.Remove(portfolioId);
                    await portfolios.Save();

                    var logs = sp.GetRequiredService<IRefreshLogService>();
                    await logs.RemoveRefreshLogsForEntity(RefreshType.Portfolio, portfolioId);
                },
                HttpContext?.RequestAborted ?? default);

            return NoContent();
        }

        [HttpPut]
        [LicenseGuard(CheckPortfolioConstraint = true)]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        public async Task<ActionResult<PortfolioSettingDto>> UpdatePortfolio(int portfolioId, PortfolioSettingDto portfolioSetting)
        {
            var baselineValidation = BaselineValidationService.Validate(
                portfolioSetting.ProcessBehaviourChartBaselineStartDate,
                portfolioSetting.ProcessBehaviourChartBaselineEndDate,
                portfolioSetting.DoneItemsCutoffDays);

            if (!baselineValidation.IsValid)
            {
                return BadRequest(baselineValidation.ErrorMessage);
            }

            var stateMappingValidation = StateMappingValidator.ValidateSettings(portfolioSetting);
            if (!stateMappingValidation.IsValid)
            {
                return BadRequest(stateMappingValidation.Errors);
            }

            if (!TeamController.IsStalenessThresholdInRange(portfolioSetting.StalenessThresholdDays))
            {
                return BadRequest($"Staleness threshold must be between {TeamController.MinStalenessThresholdDays} and {TeamController.MaxStalenessThresholdDays} days.");
            }

            if (!TeamController.IsStalenessThresholdInRange(portfolioSetting.BlockedStalenessThresholdDays))
            {
                return BadRequest($"Blocked staleness threshold must be between {TeamController.MinStalenessThresholdDays} and {TeamController.MaxStalenessThresholdDays} days.");
            }

            var portfolioForValidation = portfolioRepository.GetById(portfolioId);
            if (portfolioForValidation != null)
            {
                var blockedError = ValidateBlockedRuleSet(portfolioSetting.BlockedRuleSetJson, portfolioForValidation);
                if (blockedError != null)
                {
                    return BadRequest(blockedError);
                }
            }

            return await this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, async portfolio =>
            {
                portfolio.SyncWithPortfolioSettings(portfolioSetting, teamRepository);
                portfolio.BlockedRuleSetJson = portfolioSetting.BlockedRuleSetJson;

                portfolioRepository.Update(portfolio);

                if (portfolioSetting.ConcurrencyToken.HasValue)
                {
                    portfolioRepository.ApplyConcurrencyTokenForEdit(portfolio, portfolioSetting.ConcurrencyToken.Value);
                }

                await portfolioRepository.Save();

                var updatedPortfolioSettings = new PortfolioSettingDto(portfolio);
                return updatedPortfolioSettings;
            });
        }

        [HttpGet("settings")]
        [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
        public async Task<ActionResult<PortfolioSettingDto>> GetPortfolioSettings(int portfolioId)
        {
            return await this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, async portfolio =>
            {
                var relatedTeamIds = portfolio.Teams.Select(t => t.Id);
                if (portfolio.OwningTeam is not null)
                {
                    relatedTeamIds = relatedTeamIds.Append(portfolio.OwningTeam.Id);
                }

                var relatedTeamIdArray = relatedTeamIds.Distinct().ToArray();

                var readableTeamIds = await rbacAdministrationService
                    .GetReadableTeamIdsAsync(User, relatedTeamIdArray, HttpContext?.RequestAborted ?? default)
                    .ConfigureAwait(false);
                var readableTeamIdSet = (readableTeamIds ?? relatedTeamIdArray)
                    .ToHashSet();

                var portfolioSettingDto = new PortfolioSettingDto(portfolio, readableTeamIdSet)
                {
                    BlockedRuleSetJson = blockedItemService.GetEffectiveRuleSetJson(portfolio),
                };
                return portfolioSettingDto;
            });
        }

        private string? ValidateBlockedRuleSet(string? ruleSetJson, Portfolio portfolio)
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

            if (!blockedItemService.ValidateRuleSet(ruleSet, portfolio))
            {
                return "Blocked rule set is invalid: unknown field key, unsupported operator, value exceeds maximum length, or rule count exceeds the allowed maximum.";
            }

            return null;
        }
    }
}
