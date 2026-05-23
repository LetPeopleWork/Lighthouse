using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IDeliveryRuleService
    {
        WorkItemRuleSchema GetRuleSchema(Portfolio portfolio);

        IEnumerable<Feature> GetMatchingFeaturesForRuleset(WorkItemRuleSet ruleSet, IEnumerable<Feature> features);

        void RecomputeRuleBasedDeliveries(Portfolio portfolio, IEnumerable<Delivery> deliveries);
    }
}
