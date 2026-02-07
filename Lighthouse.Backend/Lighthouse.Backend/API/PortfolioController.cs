using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/portfolios/{portfolioId:int}")]
    [ApiController]
    public class PortfolioController(
        IRepository<Portfolio> portfolioRepository,
        IRepository<Team> teamRepository,
        IPortfolioUpdater workItemUpdateService)
        : ControllerBase
    {
        [HttpGet]
        public ActionResult<PortfolioDto> Get(int portfolioId)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, portfolio => new PortfolioDto(portfolio));
        }

        [HttpPost("refresh")]
        [LicenseGuard(CheckPortfolioConstraint = true)]
        public ActionResult UpdateFeaturesForPortfolio(int portfolioId)
        {
            workItemUpdateService.TriggerUpdate(portfolioId);

            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePortfolio(int portfolioId)
        {
            portfolioRepository.Remove(portfolioId);
            await portfolioRepository.Save();
            return NoContent();
        }

        [HttpPut]
        [LicenseGuard(CheckPortfolioConstraint = true)]
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
        public ActionResult<PortfolioSettingDto> GetPortfolioSettings(int portfolioId)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, portfolio =>
            {
                var portfolioSettingDto = new PortfolioSettingDto(portfolio);
                return portfolioSettingDto;
            });
        }
    }
}
