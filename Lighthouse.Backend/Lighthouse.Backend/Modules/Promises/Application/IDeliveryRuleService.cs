using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.Promises.Application
{
    public interface IDeliveryRuleService
    {
        WorkItemRuleSchema GetRuleSchema(Portfolio portfolio);

        IEnumerable<Feature> GetMatchingFeaturesForRuleset(WorkItemRuleSet ruleSet, IEnumerable<Feature> features);

        void RecomputeRuleBasedDeliveries(Portfolio portfolio, RecordableDeliveries deliveries);
    }
}
