using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>
    /// Which of a Team's finished work items count towards the throughput a forecast is built from.
    /// </summary>
    public interface IThroughputFilter
    {
        WorkItemRuleSet? GetEffectiveRuleSet(Team team);

        IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, WorkItemRuleSet ruleSet);
    }
}
