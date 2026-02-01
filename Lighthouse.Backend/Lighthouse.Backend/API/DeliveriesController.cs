using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Interfaces;
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
        ILicenseService licenseService,
        IDeliveryRuleService deliveryRuleService)
        : ControllerBase
    {
        [HttpGet("portfolio/{portfolioId:int}")]
        [ProducesResponseType<IEnumerable<DeliveryWithLikelihoodDto>>(StatusCodes.Status200OK)]
        public IActionResult GetByPortfolio(int portfolioId)
        {
            var deliveries = deliveryRepository.GetByPortfolioAsync(portfolioId);
            var deliveryDtos = deliveries.Select(DeliveryWithLikelihoodDto.FromDelivery);
            return Ok(deliveryDtos.OrderBy(d => d.Date));
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

            var deliveryRequestErrorCode = VerifyDeliveryRequest(portfolioId, request);
            if (deliveryRequestErrorCode != null)
            {
                return deliveryRequestErrorCode;
            }

            var portfolio = portfolioRepository.GetById(portfolioId);
            if (portfolio == null)
            {
                return NotFound($"Portfolio with ID {portfolioId} not found");
            }

            var utcDate = request.Date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.Date, DateTimeKind.Utc)
                : request.Date.ToUniversalTime();

            var delivery = new Delivery(request.Name, utcDate, portfolioId)
            {
                SelectionMode = request.SelectionMode
            };

            switch (delivery.SelectionMode)
            {
                case DeliverySelectionMode.RuleBased:
                    CreateRuleBasedDelivery(request, delivery);
                    break;
                case DeliverySelectionMode.Manual:
                    var featureNotFoundError = CreateManualFeatureSelectionDelivery(request, delivery);
                    if (featureNotFoundError != null)
                    {
                        return featureNotFoundError;
                    }
                    break;
                default:
                    throw new NotSupportedException($"Delivery Mode {delivery.SelectionMode} is not supported");
            }

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

            if (request.SelectionMode == DeliverySelectionMode.RuleBased)
            {
                var errorStatus = CheckRuleBasedDeliveryPrerequisites(request);
                if (errorStatus != null)
                {
                    return errorStatus;
                }
            }

            var utcDate = request.Date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.Date, DateTimeKind.Utc)
                : request.Date.ToUniversalTime();

            existingDelivery.Name = request.Name;
            existingDelivery.Date = utcDate;
            existingDelivery.SelectionMode = request.SelectionMode;
            
            switch (existingDelivery.SelectionMode)
            {
                case DeliverySelectionMode.RuleBased:
                    CreateRuleBasedDelivery(request, existingDelivery);
                    break;
                case DeliverySelectionMode.Manual:
                    var featureNotFoundError = CreateManualFeatureSelectionDelivery(request, existingDelivery);
                    if (featureNotFoundError != null)
                    {
                        return featureNotFoundError;
                    }
                    break;
                default:
                    throw new NotSupportedException($"Delivery Mode {existingDelivery.SelectionMode} is not supported");
            }

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

        private NotFoundObjectResult? CreateManualFeatureSelectionDelivery(UpdateDeliveryRequest request, Delivery delivery)
        {
            delivery.RuleDefinitionJson = null;
            delivery.RuleSchemaVersion = null;
            delivery.Features.Clear();
            
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
            
            delivery.Features.AddRange(featureList);

            return null;
        }

        private void CreateRuleBasedDelivery(UpdateDeliveryRequest request, Delivery delivery)
        {
            var ruleSet = new DeliveryRuleSet
            {
                Version = DeliveryRuleSet.SchemaVersion,
                Conditions = request.Rules!.Select(r => new DeliveryRuleCondition
                {
                    FieldKey = r.FieldKey,
                    Operator = r.Operator,
                    Value = r.Value
                }).ToList()
            };
            delivery.RuleDefinitionJson = JsonSerializer.Serialize(ruleSet);
            delivery.RuleSchemaVersion = DeliveryRuleSet.SchemaVersion;
            
            
            delivery.Features.Clear();
            var portfolioFeatures = GetFeaturesForPortfolio(delivery.PortfolioId);
            var matchingFeatures = deliveryRuleService.GetMatchingFeaturesForRuleset(ruleSet, portfolioFeatures);
            delivery.Features.AddRange(matchingFeatures);
        }

        private List<Feature> GetFeaturesForPortfolio(int portfolioId)
        {
            var portfolio = portfolioRepository.GetById(portfolioId);

            return portfolio == null ? [] : portfolio.Features;
        }

        private IActionResult? VerifyDeliveryRequest(int portfolioId, UpdateDeliveryRequest request)
        {
            // Rule-based selection requires premium license
            if (request.SelectionMode == DeliverySelectionMode.RuleBased)
            {
                var errorStatus = CheckRuleBasedDeliveryPrerequisites(request);
                if (errorStatus != null)
                {
                    return errorStatus;
                }
            }
            else
            {
                if (licenseService.CanUsePremiumFeatures())
                {
                    return null;
                }

                var existingDeliveries = deliveryRepository.GetByPortfolioAsync(portfolioId);
                if (existingDeliveries.Any())
                {
                    return StatusCode(403, "Free users can only have 1 delivery per portfolio");
                }
            }

            return null;
        }

        private IActionResult? CheckRuleBasedDeliveryPrerequisites(UpdateDeliveryRequest request)
        {
            if (!licenseService.CanUsePremiumFeatures())
            {
                return StatusCode(403, "Rule-based delivery selection requires a premium license");
            }

            if (request.Rules == null || request.Rules.Count == 0)
            {
                return BadRequest("At least one rule condition is required for rule-based selection");
            }

            return null;
        }
    }
}