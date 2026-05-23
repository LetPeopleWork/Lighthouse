// SCAFFOLD: true
using System;
using System.Collections.Generic;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public sealed class ForecastFilterRuleService : IForecastFilterRuleService
    {
        public WorkItemRuleSchema GetSchema(Team team)
        {
            throw new InvalidOperationException("Not yet implemented — RED scaffold (DDD-1, DDD-9). DELIVER wave will compose WorkItemFieldProvider + team-connection additional fields.");
        }

        public WorkItemRuleSet? GetEffectiveRuleSet(Team team)
        {
            throw new InvalidOperationException("Not yet implemented — RED scaffold (DDD-8, DDD-9). DELIVER wave will return null on free tenant / null JSON / zero conditions, otherwise the deserialised WorkItemRuleSet.");
        }

        public IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, WorkItemRuleSet ruleSet)
        {
            throw new InvalidOperationException("Not yet implemented — RED scaffold (D8 semantics: matched items are EXCLUDED). DELIVER wave will call IRuleEvaluator<WorkItem>.Match then exclude.");
        }

        public bool ValidateRuleSet(WorkItemRuleSet ruleSet, Team team)
        {
            throw new InvalidOperationException("Not yet implemented — RED scaffold. DELIVER wave will call IRuleEvaluator<WorkItem>.IsValid against GetSchema(team).");
        }
    }
}
