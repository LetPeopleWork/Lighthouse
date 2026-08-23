using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/portfolios/{portfolioId:int}/delivery-sources")]
    [Route("api/latest/portfolios/{portfolioId:int}/delivery-sources")]
    [ApiController]
    public class DeliverySourcesController(
        IRepository<Portfolio> portfolioRepository,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory)
        : ControllerBase
    {
        [HttpGet]
        [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
        [ProducesResponseType<List<DeliverySourceDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetDeliverySources(int portfolioId)
        {
            var portfolio = portfolioRepository.GetById(portfolioId);
            if (portfolio == null)
            {
                return NotFound(PortfolioNotFound(portfolioId));
            }

            // A connection whose connector cannot read remote delivery objects is answered with an empty
            // list rather than an error. Nothing is broken - this connection simply has nothing a date
            // could be bound to - and an empty list is what makes the extra tab disappear instead of
            // putting a tab in front of the user that can only fail.
            var provider = DeliverySourceProviderFor(portfolio);
            if (provider == null)
            {
                return Ok(new List<DeliverySourceDto>());
            }

            var sources = provider.AvailableSources(portfolio.WorkTrackingSystemConnection)
                .Select(source => new DeliverySourceDto(source.Key, source.DisplayName))
                .ToList();

            return Ok(sources);
        }

        [HttpGet("{sourceKey}/options")]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        [ProducesResponseType<List<DeliverySourceOptionDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOptions(int portfolioId, string sourceKey)
        {
            var portfolio = portfolioRepository.GetById(portfolioId);
            if (portfolio == null)
            {
                return NotFound(PortfolioNotFound(portfolioId));
            }

            var connection = portfolio.WorkTrackingSystemConnection;
            var provider = DeliverySourceProviderFor(portfolio);

            // Checked against what this connection actually offers before the remote is called, so a
            // hand-written request cannot make Lighthouse go and ask for something that does not exist
            // and then report the resulting failure as if the remote were unwell.
            if (provider == null || !provider.AvailableSources(connection).Any(source => source.Key == sourceKey))
            {
                return NotFound($"Portfolio with ID {portfolioId} offers no delivery source called '{sourceKey}'");
            }

            var options = await provider.GetOptions(connection, sourceKey);

            return Ok(options
                .Select(option => new DeliverySourceOptionDto(
                    option.Id, option.Name, option.Date, option.Project.Key, option.Project.Name,
                    option.IsSelectable, option.BlockedBecause))
                .ToList());
        }

        private static string PortfolioNotFound(int portfolioId) => $"Portfolio with ID {portfolioId} not found";

        private IDeliverySourceProvider? DeliverySourceProviderFor(Portfolio portfolio)
        {
            var connector = workTrackingConnectorFactory.GetWorkTrackingConnector(
                portfolio.WorkTrackingSystemConnection.WorkTrackingSystem);

            return connector as IDeliverySourceProvider;
        }
    }
}
