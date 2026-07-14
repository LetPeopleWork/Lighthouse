using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.WorkItems;

namespace Lighthouse.Backend.Services.Implementation.WorkItems
{
    /// <summary>
    /// Thin delegator over <see cref="IRuleEvaluator{T}"/> with Include semantics (matched ⇒ blocked),
    /// mirroring <c>ForecastFilterRuleService</c> (ADR-012 §5 / ADR-067). The Feature evaluator/provider
    /// are stateless and constructed internally (the <c>DeliveryRuleService</c> precedent) to keep the
    /// DI surface minimal — only the port itself is registered.
    /// </summary>
    public sealed class BlockedItemService(
        IRuleEvaluator<WorkItem> workItemRuleEvaluator,
        IRuleFieldProvider<WorkItem> workItemFieldProvider)
        : IBlockedItemService
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly RuleEvaluator<Feature> FeatureRuleEvaluator = new();

        private static readonly FeatureFieldProvider FeatureFieldProvider = new();

        public bool IsBlocked(WorkItem item, Team owner)
        {
            var ruleSet = GetEffectiveRuleSet(owner);
            if (ruleSet.Conditions.Count == 0)
            {
                return false;
            }

            return workItemRuleEvaluator.Match(ruleSet, [item], workItemFieldProvider).Any();
        }

        public bool IsBlocked(Feature item, Portfolio owner)
        {
            var ruleSet = GetEffectiveRuleSet(owner);
            if (ruleSet.Conditions.Count == 0)
            {
                return false;
            }

            return FeatureRuleEvaluator.Match(ruleSet, [item], FeatureFieldProvider).Any();
        }

        public WorkItemRuleSet GetEffectiveRuleSet(WorkTrackingSystemOptionsOwner owner)
        {
            if (!string.IsNullOrWhiteSpace(owner.BlockedRuleSetJson))
            {
                var stored = JsonSerializer.Deserialize<WorkItemRuleSet>(owner.BlockedRuleSetJson, JsonSerializerOptions);
                if (stored != null)
                {
                    return stored;
                }
            }

            return new WorkItemRuleSet
            {
                Version = WorkItemRuleSet.SchemaVersion,
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [],
            };
        }

        public string GetEffectiveRuleSetJson(WorkTrackingSystemOptionsOwner owner)
        {
            return JsonSerializer.Serialize(GetEffectiveRuleSet(owner), JsonSerializerOptions);
        }

        public bool ValidateRuleSet(WorkItemRuleSet ruleSet, WorkTrackingSystemOptionsOwner owner)
        {
            return workItemRuleEvaluator.IsValid(ruleSet, GetSchema(owner));
        }

        private WorkItemRuleSchema GetSchema(WorkTrackingSystemOptionsOwner owner)
        {
            var fields = FixedFieldsFor(owner).ToList();

            foreach (var additionalField in owner.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
            {
                fields.Add(new WorkItemRuleFieldDefinition
                {
                    FieldKey = $"additionalField.{additionalField.Id}",
                    DisplayName = additionalField.DisplayName,
                    IsMultiValue = false,
                });
            }

            return new WorkItemRuleSchema
            {
                Fields = fields,
                Operators = [.. RuleOperators.All],
                MaxRules = WorkItemRuleSet.MaxRules,
                MaxValueLength = WorkItemRuleSet.MaxValueLength,
            };
        }

        private IReadOnlyList<WorkItemRuleFieldDefinition> FixedFieldsFor(WorkTrackingSystemOptionsOwner owner)
        {
            return owner is Portfolio
                ? FeatureFieldProvider.GetFixedFields()
                : workItemFieldProvider.GetFixedFields();
        }
    }
}
