using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    public class PortfoliosController(
        IRepository<Portfolio> portfolioRepository,
        IRepository<Team> teamRepository,
        IPortfolioUpdater portfolioUpdater,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository,
        IRbacAdministrationService rbacAdministrationService)
        : ControllerBase
    {
        [HttpGet]
        public IEnumerable<PortfolioDto> GetPortfolios()
        {
            var portfolioDtos = new List<PortfolioDto>();

            var allPortfolios = portfolioRepository.GetAll().ToList();
            var portfolioIds = allPortfolios.Select(p => p.Id).ToArray();
            var readablePortfolioIds = rbacAdministrationService
                .GetReadablePortfolioIdsAsync(User, portfolioIds, HttpContext?.RequestAborted ?? default)
                .GetAwaiter()
                .GetResult();
            var readablePortfolioIdSet = (readablePortfolioIds ?? portfolioIds).ToHashSet();
            var teamIds = allPortfolios.SelectMany(p => p.Teams).Select(t => t.Id).Distinct().ToArray();
            var readableTeamIdSet = rbacAdministrationService
                .GetReadableTeamIdsAsync(User, teamIds, HttpContext?.RequestAborted ?? default)
                .GetAwaiter()
                .GetResult();
            var effectiveReadableTeamIds = (readableTeamIdSet ?? teamIds)
                .ToHashSet();

            foreach (var portfolio in allPortfolios.Where(portfolio => readablePortfolioIdSet.Contains(portfolio.Id)))
            {
                var portfolioDto = new PortfolioDto(portfolio, effectiveReadableTeamIds);
                portfolioDtos.Add(portfolioDto);
            }

            return portfolioDtos;
        }

        [HttpPost("refresh-all")]
        [LicenseGuard(RequirePremium = true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public ActionResult UpdateAllPortfolios()
        {
            var portfolios = portfolioRepository.GetAll();

            foreach (var portfolio in portfolios)
            {
                portfolioUpdater.TriggerUpdate(portfolio.Id);
            }

            return Ok();
        }

        [HttpPost]
        [LicenseGuard(CheckPortfolioConstraint = true, PortfolioLimitOverride = 0)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<ActionResult<PortfolioSettingDto>> CreatePortfolio(PortfolioSettingDto portfolioSetting, CancellationToken cancellationToken = default)
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

            portfolioSetting.Id = 0;

            var newPortfolio = new Portfolio();
            newPortfolio.SyncWithPortfolioSettings(portfolioSetting, teamRepository);

            portfolioRepository.Add(newPortfolio);
            await portfolioRepository.Save();

            var portfolioSettingDto = new PortfolioSettingDto(newPortfolio);
            return Ok(portfolioSettingDto);
        }

        [HttpPost("validate")]
        [LicenseGuard(CheckPortfolioConstraint = true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<ActionResult<object>> ValidatePortfolioSettings(PortfolioSettingDto portfolioSettingDto, CancellationToken cancellationToken = default)
        {
            var workTrackingSystem = workTrackingSystemConnectionRepository.GetById(portfolioSettingDto.WorkTrackingSystemConnectionId);
            if (workTrackingSystem == null)
            {
                return NotFound();
            }

            var portfolio = new Portfolio { WorkTrackingSystemConnection = workTrackingSystem };
            portfolio.SyncWithPortfolioSettings(portfolioSettingDto, teamRepository);

            var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(portfolio.WorkTrackingSystemConnection.WorkTrackingSystem);

            var validationResult = await workItemService.ValidatePortfolioSettings(portfolio);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult);
            }

            return Ok(validationResult);
        }
    }
}
