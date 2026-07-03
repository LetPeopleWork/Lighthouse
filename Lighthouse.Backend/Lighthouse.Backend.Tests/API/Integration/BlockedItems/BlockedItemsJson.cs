using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL (Epic 5074) — shared, pure JSON/rule builders for the blocked-items acceptance suite.
    /// SSOT for the mechanical shape of a blocked rule set (Mandate-12): the domain vocabulary
    /// ("State equals X", "Tags contains Y", "the flagged field is not empty") is expressed here once
    /// and reused by every slice's step methods. No HTTP, no business logic — just serialisation.
    /// </summary>
    internal static class BlockedItemsJson
    {
        /// <summary>A single blocked rule condition in domain terms: field + operator + value.</summary>
        internal readonly record struct BlockedCondition(string FieldKey, string Operator, string Value);

        internal static BlockedCondition StateEquals(string state) => new("workitem.state", "equals", state);

        internal static BlockedCondition TagsContains(string tag) => new("workitem.tags", "contains", tag);

        internal static BlockedCondition FieldIsNotEmpty(int additionalFieldId)
            => new($"additionalField.{additionalFieldId}", "isnotempty", string.Empty);

        internal static BlockedCondition UnknownField() => new("workitem.bogus", "equals", "anything");

        /// <summary>
        /// Serialise a blocked rule set (Include semantics: a matched item is blocked). Mirrors the
        /// existing WorkItemRuleSet JSON idiom already used by the forecast-filter and delivery rules.
        /// </summary>
        internal static string RuleSet(string mode, params BlockedCondition[] conditions)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Version = WorkItemRuleSet.SchemaVersion,
                Mode = mode,
                Conditions = conditions
                    .Select(c => new WorkItemRuleCondition
                    {
                        FieldKey = c.FieldKey,
                        Operator = c.Operator,
                        Value = c.Value,
                    })
                    .ToList(),
            };

            return JsonSerializer.Serialize(ruleSet);
        }

        internal static string OrRuleSet(params BlockedCondition[] conditions)
            => RuleSet(WorkItemRuleSet.ModeOr, conditions);

        /// <summary>Deserialise a persisted/served blocked rule set for structural assertions.</summary>
        internal static WorkItemRuleSet? Parse(string? json)
            => string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<WorkItemRuleSet>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        /// <summary>
        /// Take a serialised TeamSettingDto and attach the blocked rule set as a raw JSON member.
        /// Attaching it at the JSON layer (not via a typed DTO property) keeps the acceptance tests
        /// compiling against today's contract while still driving the not-yet-existing blockedRuleSet
        /// member — so the tests fail RED (missing behaviour) rather than BROKEN (missing type).
        /// </summary>
        internal static JsonObject WithBlockedRuleSet(JsonObject settingsPayload, string ruleSetJson)
        {
            settingsPayload["blockedRuleSetJson"] = ruleSetJson;
            return settingsPayload;
        }

        internal static JsonObject WithBlockedStalenessThreshold(JsonObject settingsPayload, int days)
        {
            settingsPayload["blockedStalenessThresholdDays"] = days;
            return settingsPayload;
        }

        internal static JsonObject ToJsonObject<T>(T dto)
        {
            var serialized = JsonSerializer.Serialize(dto);
            return JsonNode.Parse(serialized)!.AsObject();
        }
    }
}
