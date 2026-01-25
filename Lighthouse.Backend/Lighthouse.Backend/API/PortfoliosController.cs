using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortfoliosController(
        IRepository<Portfolio> portfolioRepository,
        IRepository<Team> teamRepository,
        IPortfolioUpdater workItemUpdateService,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemConnectionRepository)
        : ControllerBase
    {
        [HttpGet]
        public IEnumerable<PortfolioDto> GetPortfolios()
        {
            var portfolioDtos = new List<PortfolioDto>();

            var allPortfolios = portfolioRepository.GetAll();

            foreach (var portfolio in allPortfolios)
            {
                var portfolioDto = new PortfolioDto(portfolio);
                portfolioDtos.Add(portfolioDto);
            }

            return portfolioDtos;
        }

        [HttpPost("refresh-all")]
        [LicenseGuard(RequirePremium = true)]
        public ActionResult UpdateAllPortfolios()
        {
            var portfolios = portfolioRepository.GetAll();

            foreach (var portfolio in portfolios)
            {
                workItemUpdateService.TriggerUpdate(portfolio.Id);
            }

            return Ok();
        }

        [HttpPost]
        [LicenseGuard(CheckPortfolioConstraint = true, PortfolioLimitOverride = 0)]
        public async Task<ActionResult<PortfolioSettingDto>> CreatePortfolio(PortfolioSettingDto portfolioSetting)
        {
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
        public async Task<ActionResult<bool>> ValidatePortfolioSettings(PortfolioSettingDto portfolioSettingDto)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemConnectionRepository, portfolioSettingDto.WorkTrackingSystemConnectionId, async workTrackingSystem =>
            {
                var portfolio = new Portfolio { WorkTrackingSystemConnection = workTrackingSystem };
                portfolio.SyncWithPortfolioSettings(portfolioSettingDto, teamRepository);

                var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(portfolio.WorkTrackingSystemConnection.WorkTrackingSystem);

                var result = await workItemService.ValidatePortfolioSettings(portfolio);

                return result;
            });
        }
    }
}
