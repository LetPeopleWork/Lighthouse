using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.WorkItems;

namespace Lighthouse.Backend.API.Helpers
{
    /// <summary>
    /// Rejects a rule set a settings save would otherwise store unchecked. Returns null when the
    /// payload is acceptable, otherwise the message the caller hands back as a bad request.
    /// </summary>
    public static class RuleSetValidation
    {
        private const string BlockedLabel = "Blocked rule set";

        private const string ForecastFilterLabel = "Forecast filter rule set";

        private const string ReadyStageLabel = "'Ready when' rule";

        private const string BeingRefinedStageLabel = "'Being refined when' rule";

        public static string? ValidateBlockedRuleSet(
            string? ruleSetJson,
            WorkTrackingSystemOptionsOwner owner,
            IBlockedItemService blockedItemService)
        {
            return Validate(ruleSetJson, BlockedLabel, ruleSet => blockedItemService.ValidateRuleSet(ruleSet, owner));
        }

        public static string? ValidateForecastFilterRuleSet(
            string? ruleSetJson,
            Team team,
            IForecastFilterRuleService forecastFilterRuleService)
        {
            return Validate(ruleSetJson, ForecastFilterLabel, ruleSet => forecastFilterRuleService.ValidateRuleSet(ruleSet, team));
        }

        public static string? ValidateStageRules(
            StageRulesDto? stageRules,
            Team team,
            IForecastFilterRuleService forecastFilterRuleService)
        {
            if (stageRules == null)
            {
                return null;
            }

            bool IsValid(WorkItemRuleSet ruleSet) => forecastFilterRuleService.ValidateRuleSet(ruleSet, team);

            return Validate(stageRules.Ready?.ToRuleSet(), ReadyStageLabel, IsValid)
                ?? Validate(stageRules.BeingRefined?.ToRuleSet(), BeingRefinedStageLabel, IsValid);
        }

        private static string? Validate(string? ruleSetJson, string label, Func<WorkItemRuleSet, bool> isValid)
        {
            if (string.IsNullOrWhiteSpace(ruleSetJson))
            {
                return null;
            }

            if (!WorkItemRuleSetJson.TryDeserialize(ruleSetJson, out var ruleSet))
            {
                return $"{label} is not valid JSON.";
            }

            return Validate(ruleSet, label, isValid);
        }

        private static string? Validate(WorkItemRuleSet? ruleSet, string label, Func<WorkItemRuleSet, bool> isValid)
        {
            if (ruleSet == null || ruleSet.Conditions.Count == 0)
            {
                return null;
            }

            return isValid(ruleSet)
                ? null
                : $"{label} is invalid: unknown field key, unsupported operator, value exceeds maximum length, or rule count exceeds the allowed maximum.";
        }
    }
}
