using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveriesController : ControllerBase
    {
        private readonly IDeliveryRepository deliveryRepository;
        private readonly IRepository<Feature> featureRepository;
        private readonly ILicenseService licenseService;

        public DeliveriesController(
            IDeliveryRepository deliveryRepository,
            IRepository<Feature> featureRepository,
            ILicenseService licenseService)
        {
            this.deliveryRepository = deliveryRepository;
            this.featureRepository = featureRepository;
            this.licenseService = licenseService;
        }

        [HttpGet("portfolio/{portfolioId}")]
        public IActionResult GetByPortfolio(int portfolioId)
        {
            var deliveries = deliveryRepository.GetByPortfolioAsync(portfolioId);
            return Ok(deliveries);
        }

        [HttpPost("portfolio/{portfolioId}")]
        public async Task<IActionResult> CreateDelivery(
            int portfolioId, 
            [FromBody] CreateDeliveryRequest request)
        {
            // Validate date is in future
            if (request.Date <= DateTime.UtcNow)
            {
                return BadRequest("Delivery date must be in the future");
            }

            // Check licensing constraints
            if (!licenseService.CanUsePremiumFeatures())
            {
                var existingDeliveries = deliveryRepository.GetByPortfolioAsync(portfolioId);
                if (existingDeliveries.Any())
                {
                    return StatusCode(403, "Free users can only have 1 delivery per portfolio");
                }
            }

            try
            {
                // Create delivery
                var delivery = new Delivery(request.Name, request.Date, portfolioId);

                // Add features to delivery
                foreach (var featureId in request.FeatureIds)
                {
                    var feature = featureRepository.GetById(featureId);
                    if (feature != null)
                    {
                        delivery.Features.Add(feature);
                    }
                }

                deliveryRepository.Add(delivery);
                await deliveryRepository.Save();

                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{deliveryId}")]
        public async Task<IActionResult> DeleteDelivery(int deliveryId)
        {
            var delivery = deliveryRepository.GetById(deliveryId);
            if (delivery == null)
            {
                return NotFound();
            }

            deliveryRepository.Remove(deliveryId);
            await deliveryRepository.Save();

            return NoContent();
        }
    }
}