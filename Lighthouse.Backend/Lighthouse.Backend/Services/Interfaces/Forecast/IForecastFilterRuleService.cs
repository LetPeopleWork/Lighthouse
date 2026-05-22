// SCAFFOLD: true
using System.Collections.Generic;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;

namespace Lighthouse.Backend.Services.Interfaces.Forecast
{
    public interface IForecastFilterRuleService
    {
        DeliveryRuleSchema GetSchema(Team team);

        DeliveryRuleSet? GetEffectiveRuleSet(Team team);

        IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, DeliveryRuleSet ruleSet);

        bool ValidateRuleSet(DeliveryRuleSet ruleSet, Team team);
    }
}
