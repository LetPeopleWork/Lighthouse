using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/portfolios/{portfolioId:int}/delivery-rules")]
    [ApiController]
    public class DeliveryRulesController(
        IRepository<Portfolio> portfolioRepository,
        IDeliveryRuleService deliveryRuleService)
        : ControllerBase
    {
        [HttpGet("schema")]
        [ProducesResponseType<DeliveryRuleSchema>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetSchema(int portfolioId)
        {
            var portfolio = portfolioRepository.GetById(portfolioId);
            if (portfolio == null)
            {
                return NotFound($"Portfolio with ID {portfolioId} not found");
            }

            var schema = deliveryRuleService.GetRuleSchema(portfolio);
            return Ok(schema);
        }

        [HttpPost("validate")]
        [LicenseGuard(RequirePremium = true)]
        [ProducesResponseType<List<FeatureDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<List<FeatureDto>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Validate(int portfolioId, [FromBody] ValidateDeliveryRulesRequest request)
        {
            var portfolio = portfolioRepository.GetById(portfolioId);
            if (portfolio == null)
            {
                return NotFound($"Portfolio with ID {portfolioId} not found");
            }

            var ruleSet = ConvertToRuleSet(request);
            var result = deliveryRuleService.GetMatchingFeaturesForRuleset(ruleSet, portfolio.Features);
            
            var matchingFeatures = new List<FeatureDto>(result.Select(f => new FeatureDto(f)));

            if (matchingFeatures.Count == 0)
            {
                return BadRequest(matchingFeatures);
            }

            return Ok(matchingFeatures);
        }

        private static DeliveryRuleSet ConvertToRuleSet(ValidateDeliveryRulesRequest request)
        {
            return new DeliveryRuleSet
            {
                Conditions = request.Rules.Select(r => new DeliveryRuleCondition
                {
                    FieldKey = r.FieldKey,
                    Operator = r.Operator,
                    Value = r.Value
                }).ToList()
            };
        }
    }
}
