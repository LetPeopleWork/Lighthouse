using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/portfolios/{portfolioId:int}/delivery-sources")]
    [Route("api/latest/portfolios/{portfolioId:int}/delivery-sources")]
    [ApiController]
    public class DeliverySourcesController(
        IRepository<Portfolio> portfolioRepository,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        IDeliverySourceResolver deliverySourceResolver,
        IBlackoutPeriodService blackoutPeriodService,
        IBlockedItemService blockedItemService,
        ILighthouseClock clock)
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

            var provider = ProviderOffering(portfolio, sourceKey);
            if (provider == null)
            {
                return NotFound(SourceNotOffered(portfolioId, sourceKey));
            }

            var options = await provider.GetOptions(portfolio.WorkTrackingSystemConnection, sourceKey);

            return Ok(options
                .Select(option => new DeliverySourceOptionDto(
                    option.Id, option.Name, option.Date, option.Project.Key, option.Project.Name,
                    option.IsSelectable, option.BlockedBecause))
                .ToList());
        }

        [HttpPost("{sourceKey}/preview")]
        [LicenseGuard(RequirePremium = true)]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        [ProducesResponseType<DeliverySourcePreviewDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Preview(int portfolioId, string sourceKey, [FromBody] PreviewDeliverySourceRequest request)
        {
            var portfolio = portfolioRepository.GetById(portfolioId);
            if (portfolio == null)
            {
                return NotFound(PortfolioNotFound(portfolioId));
            }

            if (ProviderOffering(portfolio, sourceKey) == null)
            {
                return NotFound(SourceNotOffered(portfolioId, sourceKey));
            }

            var previews = await deliverySourceResolver.ResolveForPortfolio(portfolio, sourceKey, [request.SourceReference]);
            if (!previews.TryGetValue(request.SourceReference, out var preview))
            {
                return CouldNotBeAsked(request.SourceReference);
            }

            // A source this Portfolio matches nothing in is an answer to the question that was asked, not
            // a failed request, and it is answered with the reason the list came back empty. The rules
            // route next door returns 400 for its own empty list; copying that here would turn the state
            // this screen exists to explain into an error the client can only render as a failure.
            return preview.Resolution switch
            {
                DeliverySourceResolution.Resolved resolved => Ok(PreviewOf(resolved.Snapshot, preview)),
                DeliverySourceResolution.NoDate noDate => BadRequest($"'{noDate.Name}' carries no date, so there is no date to preview"),
                DeliverySourceResolution.NotFound => NotFound($"The delivery source '{request.SourceReference}' no longer exists"),
                _ => CouldNotBeAsked(request.SourceReference),
            };
        }

        private ObjectResult CouldNotBeAsked(string sourceReference)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                $"The delivery source '{sourceReference}' could not be read right now");
        }

        private DeliverySourcePreviewDto PreviewOf(DeliverySourceSnapshot snapshot, PortfolioSourcePreview preview)
        {
            return new DeliverySourcePreviewDto(
                snapshot.Name, snapshot.Date, FeaturesComingAlong(preview.TrackedFeatures), EmptyReasonFor(preview));
        }

        private static DeliverySourcePreviewEmptyReason EmptyReasonFor(PortfolioSourcePreview preview)
        {
            if (preview.TrackedFeatures.Count > 0)
            {
                return DeliverySourcePreviewEmptyReason.None;
            }

            return preview.TaggedItemCount == 0
                ? DeliverySourcePreviewEmptyReason.NothingTaggedAgainstTheSource
                : DeliverySourcePreviewEmptyReason.TaggedWorkNotTrackedByThisPortfolio;
        }

        private List<FeatureDto> FeaturesComingAlong(IReadOnlyList<Feature> features)
        {
            var forecastWindowStart = clock.TodayAsUtcMidnight;
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                forecastWindowStart, FeatureForecastWindow.EndFor(forecastWindowStart, features));

            return features
                .Select(feature => new FeatureDto(
                    feature, clock, blackoutPeriods,
                    feature.Portfolios.Any(portfolio => blockedItemService.IsBlocked(feature, portfolio)), null))
                .ToList();
        }

        private static string PortfolioNotFound(int portfolioId) => $"Portfolio with ID {portfolioId} not found";

        private static string SourceNotOffered(int portfolioId, string sourceKey) =>
            $"Portfolio with ID {portfolioId} offers no delivery source called '{sourceKey}'";

        private IDeliverySourceProvider? DeliverySourceProviderFor(Portfolio portfolio)
        {
            var connector = workTrackingConnectorFactory.GetWorkTrackingConnector(
                portfolio.WorkTrackingSystemConnection.WorkTrackingSystem);

            return connector as IDeliverySourceProvider;
        }

        /// <summary>
        /// The connector behind this Portfolio, but only if it says it offers the source that was asked
        /// for. Checked before the remote is called, so a hand-written request cannot make Lighthouse go
        /// and ask for something that does not exist and then report the resulting failure as if the
        /// remote were unwell.
        /// </summary>
        private IDeliverySourceProvider? ProviderOffering(Portfolio portfolio, string sourceKey)
        {
            var provider = DeliverySourceProviderFor(portfolio);
            var offersIt = provider?.AvailableSources(portfolio.WorkTrackingSystemConnection)
                .Any(source => source.Key == sourceKey) == true;

            return offersIt ? provider : null;
        }
    }
}
