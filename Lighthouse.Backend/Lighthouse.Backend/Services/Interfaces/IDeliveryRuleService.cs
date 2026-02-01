using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IDeliveryRuleService
    {
        DeliveryRuleSchema GetRuleSchema(Portfolio portfolio);

        IEnumerable<Feature> GetMatchingFeaturesForRuleset(DeliveryRuleSet ruleSet, IEnumerable<Feature> features);

        void RecomputeRuleBasedDeliveries(Portfolio portfolio, IEnumerable<Delivery> deliveries);
    }
}
