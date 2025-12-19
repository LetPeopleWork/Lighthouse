using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveriesController(
        IDeliveryRepository deliveryRepository,
        IRepository<Feature> featureRepository,
        IRepository<Portfolio> portfolioRepository,
        ILicenseService licenseService)
        : ControllerBase
    {
        [HttpGet("portfolio/{portfolioId:int}")]
        public IActionResult GetByPortfolio(int portfolioId)
        {
            var deliveries = deliveryRepository.GetByPortfolioAsync(portfolioId);
            var deliveryDtos = deliveries.Select(DeliveryWithLikelihoodDto.FromDelivery);
            return Ok(deliveryDtos);
        }
        
        [HttpGet]
        public IActionResult GetAll()
        {
            var deliveries = deliveryRepository.GetAll();
            var deliveryDtos = deliveries.Select(DeliveryWithLikelihoodDto.FromDelivery);
            return Ok(deliveryDtos);
        }

        [HttpPost("portfolio/{portfolioId:int}")]
        public async Task<IActionResult> CreateDelivery(
            int portfolioId,
            [FromBody] UpdateDeliveryRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("Name is required");
            }
            
            if (request.Date <= DateTime.UtcNow)
            {
                return BadRequest("Delivery date must be in the future");
            }

            if (!licenseService.CanUsePremiumFeatures())
            {
                var existingDeliveries = deliveryRepository.GetByPortfolioAsync(portfolioId);
                if (existingDeliveries.Any())
                {
                    return StatusCode(403, "Free users can only have 1 delivery per portfolio");
                }
            }
            
            var featureList = new List<Feature>();
            foreach (var featureId in request.FeatureIds)
            {
                var feature = featureRepository.GetById(featureId);
                if (feature == null)
                {
                    return NotFound($"Feature with ID {featureId} does not exist");
                }
                
                featureList.Add(feature);
            }
            
            var portfolio = portfolioRepository.GetById(portfolioId);
            if (portfolio == null)
            {
                return NotFound($"Portfolio with ID {portfolioId} not found");
            }
            
            var delivery = new Delivery(request.Name, request.Date, portfolioId);
            delivery.Features.AddRange(featureList);

            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return Ok();
        }

        [HttpPut("{deliveryId:int}")]
        public async Task<IActionResult> UpdateDelivery(
            int deliveryId,
            [FromBody] UpdateDeliveryRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("Name is required");
            }
            
            if (request.Date <= DateTime.UtcNow)
            {
                return BadRequest("Delivery date must be in the future");
            }

            var existingDelivery = deliveryRepository.GetById(deliveryId);
            if (existingDelivery == null)
            {
                return NotFound($"Delivery with ID {deliveryId} not found");
            }
            
            var featureList = new List<Feature>();
            foreach (var featureId in request.FeatureIds)
            {
                var feature = featureRepository.GetById(featureId);
                if (feature == null)
                {
                    return NotFound($"Feature with ID {featureId} does not exist");
                }
                
                featureList.Add(feature);
            }
            
            existingDelivery.Name = request.Name;
            existingDelivery.Date = request.Date;
            existingDelivery.Features.Clear();
            existingDelivery.Features.AddRange(featureList);

            await deliveryRepository.Save();

            return Ok();
        }

        [HttpDelete("{deliveryId:int}")]
        public async Task<IActionResult> DeleteDelivery(int deliveryId)
        {
            deliveryRepository.Remove(deliveryId);
            await deliveryRepository.Save();

            return NoContent();
        }
    }
}