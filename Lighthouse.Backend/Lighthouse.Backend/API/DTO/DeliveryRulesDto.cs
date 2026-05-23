using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.API.DTO
{
    public class ValidateDeliveryRulesRequest
    {
        public List<WorkItemRuleCondition> Rules { get; set; } = [];
    }
}
