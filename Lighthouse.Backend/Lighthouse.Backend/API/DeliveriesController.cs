using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    public class DeliveriesController(
        IDeliveryRepository deliveryRepository,
        IRepository<Portfolio> portfolioRepository,
        ILicenseService licenseService,
        IDeliveryRuleService deliveryRuleService,
        IRbacAdministrationService rbacAdministrationService)
        : ControllerBase
    {
        [HttpGet("portfolio/{portfolioId:int}")]
        [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
        [ProducesResponseType<IEnumerable<DeliveryWithLikelihoodDto>>(StatusCodes.Status200OK)]
        public IActionResult GetByPortfolio(int portfolioId)
        {
            var deliveries = deliveryRepository.GetByPortfolioAsync(portfolioId);
            var deliveryDtos = deliveries.Select(DeliveryWithLikelihoodDto.FromDelivery);
            return Ok(deliveryDtos.OrderBy(d => d.Date));
        }

        [HttpPost("portfolio/{portfolioId:int}")]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
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

            var existingDelivery = deliveryRepository.GetByIdForUpdate(deliveryId);
            if (existingDelivery == null)
            {
                return NotFound($"Delivery with ID {deliveryId} not found");
            }

            if (!await rbacAdministrationService.CanSatisfyRequirementAsync(
                    User,
                    RbacGuardRequirement.PortfolioWrite,
                    existingDelivery.PortfolioId,
                    HttpContext?.RequestAborted ?? default))
            {
                return Forbid();
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

            if (request.ConcurrencyToken.HasValue)
            {
                deliveryRepository.SetOriginalConcurrencyToken(existingDelivery, request.ConcurrencyToken.Value);
            }

            await deliveryRepository.Save();

            return Ok();
        }

        [HttpDelete("{deliveryId:int}")]
        public async Task<IActionResult> DeleteDelivery(int deliveryId)
        {
            var portfolioId = deliveryRepository.GetPortfolioId(deliveryId);
            if (!portfolioId.HasValue)
            {
                return NotFound();
            }

            if (!await rbacAdministrationService.CanSatisfyRequirementAsync(
                    User,
                    RbacGuardRequirement.PortfolioWrite,
                    portfolioId.Value,
                    HttpContext?.RequestAborted ?? default))
            {
                return Forbid();
            }

            deliveryRepository.Remove(deliveryId);
            await deliveryRepository.Save();

            return NoContent();
        }

        private NotFoundObjectResult? CreateManualFeatureSelectionDelivery(UpdateDeliveryRequest request, Delivery delivery)
        {
            delivery.RuleDefinitionJson = null;
            delivery.RuleSchemaVersion = null;
            delivery.Features.Clear();

            var featureList = deliveryRepository.GetFeaturesByIds(request.FeatureIds);

            var missingIds = request.FeatureIds
                .Except(featureList.Select(f => f.Id))
                .ToList();

            if (missingIds.Count != 0)
            {
                return NotFound($"Feature with ID {missingIds[0]} does not exist");
            }

            delivery.Features.AddRange(featureList);
            return null;
        }

        private void CreateRuleBasedDelivery(UpdateDeliveryRequest request, Delivery delivery)
        {
            var mode = string.Equals(request.Mode, WorkItemRuleSet.ModeOr, StringComparison.OrdinalIgnoreCase)
                ? WorkItemRuleSet.ModeOr
                : WorkItemRuleSet.ModeAnd;
            var ruleSet = new WorkItemRuleSet
            {
                Version = WorkItemRuleSet.SchemaVersion,
                Mode = mode,
                Conditions = request.Rules!.Select(r => new WorkItemRuleCondition
                {
                    FieldKey = r.FieldKey,
                    Operator = r.Operator,
                    Value = r.Value
                }).ToList()
            };
            delivery.RuleDefinitionJson = JsonSerializer.Serialize(ruleSet);
            delivery.RuleSchemaVersion = WorkItemRuleSet.SchemaVersion;


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