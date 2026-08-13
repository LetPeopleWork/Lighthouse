using System.Text.Json;

namespace Lighthouse.Backend.Models.WorkItemRules
{
    /// <summary>
    /// The one way a rule set crosses the boundary between a database column and an object.
    /// Four columns store rule sets — blocked rules on a team and on a portfolio, a team's
    /// forecast filter, a delivery's rule definition — and each end used to bring its own
    /// serializer settings. Where a writer and a reader disagreed about casing the rule set
    /// came back with no conditions at all, which reads as "no rules configured" rather than
    /// as an error. Reading accepts either casing so rows written before this existed still
    /// load; writing always emits camelCase, matching what the browser sends.
    /// </summary>
    public static class WorkItemRuleSetJson
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string Serialize(WorkItemRuleSet ruleSet)
        {
            return JsonSerializer.Serialize(ruleSet, Options);
        }

        public static WorkItemRuleSet? Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<WorkItemRuleSet>(json, Options);
        }

        public static bool TryDeserialize(string? json, out WorkItemRuleSet? ruleSet)
        {
            try
            {
                ruleSet = Deserialize(json);
                return true;
            }
            catch (JsonException)
            {
                ruleSet = null;
                return false;
            }
        }
    }
}
