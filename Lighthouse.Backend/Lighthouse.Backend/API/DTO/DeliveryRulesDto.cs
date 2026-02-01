using Lighthouse.Backend.Models.DeliveryRules;

namespace Lighthouse.Backend.API.DTO
{
    public class ValidateDeliveryRulesRequest
    {
        public List<DeliveryRuleCondition> Rules { get; set; } = [];
    }
}
