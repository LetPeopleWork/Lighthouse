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

        /// <summary>
        /// The stored filter as the settings screen should see it: conditions naming a field the
        /// connection no longer defines are dropped, so the editor never shows a row that would
        /// fail validation on the way back. Null when the team has no filter left to show.
        /// </summary>
        string? GetStoredRuleSetJsonForEditing(Team team);

        IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, WorkItemRuleSet ruleSet);

        bool ValidateRuleSet(WorkItemRuleSet ruleSet, Team team);
    }
}
