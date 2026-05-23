// SCAFFOLD: true
using System.Collections.Generic;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.Services.Interfaces.Forecast
{
    public interface IForecastFilterRuleService
    {
        WorkItemRuleSchema GetSchema(Team team);

        WorkItemRuleSet? GetEffectiveRuleSet(Team team);

        IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, WorkItemRuleSet ruleSet);

        bool ValidateRuleSet(WorkItemRuleSet ruleSet, Team team);
    }
}
