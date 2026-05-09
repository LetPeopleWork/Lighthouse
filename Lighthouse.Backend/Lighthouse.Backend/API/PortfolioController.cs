using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/portfolios/{portfolioId:int}")]
    [Route("api/latest/portfolios/{portfolioId:int}")]
    [ApiController]
    public class PortfolioController(
        IRepository<Portfolio> portfolioRepository,
        IRepository<Team> teamRepository,
        IPortfolioUpdater portfolioUpdater,
        IRefreshLogService refreshLogService,
        IRbacAdministrationService rbacAdministrationService)
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
            portfolioRepository.Remove(portfolioId);
            await portfolioRepository.Save();

            await refreshLogService.RemoveRefreshLogsForEntity(RefreshType.Portfolio, portfolioId);

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

            return await this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, async portfolio =>
            {
                portfolio.SyncWithPortfolioSettings(portfolioSetting, teamRepository);

                portfolioRepository.Update(portfolio);
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

                var portfolioSettingDto = new PortfolioSettingDto(portfolio, readableTeamIdSet);
                return portfolioSettingDto;
            });
        }
    }
}
