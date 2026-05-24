using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;

namespace Lighthouse.Backend.Services.Implementation.WorkItemRules
{
    public class RuleEvaluator<T> : IRuleEvaluator<T> where T : class
    {
        private const string TagsFieldKey = "feature.tags";

        public IEnumerable<T> Match(WorkItemRuleSet ruleSet, IEnumerable<T> items, IRuleFieldProvider<T> fieldProvider)
        {
            if (RuleSetHasError(ruleSet, fieldProvider))
            {
                return [];
            }

            var useOrMode = IsOrMode(ruleSet.Mode);
            return items.Where(item => MatchesGroup(item, ruleSet.Conditions, fieldProvider, useOrMode)).ToList();
        }

        public bool IsValid(WorkItemRuleSet ruleSet, WorkItemRuleSchema schema)
        {
            if (ruleSet.Conditions.Count == 0 || ruleSet.Conditions.Count > schema.MaxRules)
            {
                return false;
            }

            var allowedFieldKeys = schema.Fields
                .Select(f => f.FieldKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allowedOperators = schema.Operators
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return ruleSet.Conditions.All(c => IsConditionValid(c, allowedFieldKeys, allowedOperators, schema.MaxValueLength));
        }

        private static bool IsConditionValid(
            WorkItemRuleCondition condition,
            HashSet<string> allowedFieldKeys,
            HashSet<string> allowedOperators,
            int maxValueLength)
        {
            if (!allowedFieldKeys.Contains(condition.FieldKey))
            {
                return false;
            }

            if (!allowedOperators.Contains(condition.Operator))
            {
                return false;
            }

            return condition.Value.Length <= maxValueLength;
        }

        private static bool RuleSetHasError(WorkItemRuleSet ruleSet, IRuleFieldProvider<T> fieldProvider)
        {
            if (ruleSet.Conditions.Count is 0 or > WorkItemRuleSet.MaxRules)
            {
                return true;
            }

            var allowedFieldKeys = fieldProvider.GetFixedFields()
                .Select(f => f.FieldKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return ruleSet.Conditions.Any(c => !IsConditionWellFormed(c, allowedFieldKeys));
        }

        private static bool IsConditionWellFormed(WorkItemRuleCondition condition, HashSet<string> fixedFieldKeys)
        {
            if (!IsKnownFieldKey(condition.FieldKey, fixedFieldKeys))
            {
                return false;
            }

            if (!IsKnownOperator(condition.Operator))
            {
                return false;
            }

            return condition.Value.Length <= WorkItemRuleSet.MaxValueLength;
        }

        private static bool IsKnownFieldKey(string fieldKey, HashSet<string> fixedFieldKeys)
        {
            if (fixedFieldKeys.Contains(fieldKey))
            {
                return true;
            }

            const string additionalFieldBaseKey = "additionalField.";
            if (!fieldKey.StartsWith(additionalFieldBaseKey, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var idPart = fieldKey[additionalFieldBaseKey.Length..];
            return int.TryParse(idPart, out _);
        }

        private static bool IsKnownOperator(string op)
        {
            var normalised = op.ToLowerInvariant();
            return normalised is RuleOperators.Equals
                or RuleOperators.NotEquals
                or RuleOperators.Contains
                or RuleOperators.NotContains
                or RuleOperators.IsEmpty
                or RuleOperators.IsNotEmpty;
        }

        private static bool IsOrMode(string mode)
        {
            return string.Equals(mode, WorkItemRuleSet.ModeOr, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesGroup(T item, List<WorkItemRuleCondition> conditions, IRuleFieldProvider<T> fieldProvider, bool useOrMode)
        {
            return useOrMode
                ? conditions.Any(c => EvaluateCondition(item, c, fieldProvider))
                : conditions.All(c => EvaluateCondition(item, c, fieldProvider));
        }

        private static bool EvaluateCondition(T item, WorkItemRuleCondition condition, IRuleFieldProvider<T> fieldProvider)
        {
            var op = condition.Operator.ToLowerInvariant();

            if (condition.FieldKey.Equals(TagsFieldKey, StringComparison.OrdinalIgnoreCase))
            {
                var tags = fieldProvider.GetTagsForField(item, condition.FieldKey);
                return EvaluateTagsCondition(tags, op, condition.Value);
            }

            var fieldValue = fieldProvider.GetFieldValue(item, condition.FieldKey);
            return op switch
            {
                RuleOperators.Equals => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                RuleOperators.NotEquals => !string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                RuleOperators.Contains => fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
                RuleOperators.NotContains => !fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
                RuleOperators.IsEmpty => string.IsNullOrEmpty(fieldValue),
                RuleOperators.IsNotEmpty => !string.IsNullOrEmpty(fieldValue),
                _ => false,
            };
        }

        private static bool EvaluateTagsCondition(IReadOnlyList<string> tags, string op, string value)
        {
            return op switch
            {
                RuleOperators.Equals => tags.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase)),
                RuleOperators.NotEquals => !tags.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase)),
                RuleOperators.Contains => tags.Any(t => t.Contains(value, StringComparison.OrdinalIgnoreCase)),
                RuleOperators.NotContains => !tags.Any(t => t.Contains(value, StringComparison.OrdinalIgnoreCase)),
                RuleOperators.IsEmpty => tags.Count == 0,
                RuleOperators.IsNotEmpty => tags.Count > 0,
                _ => false,
            };
        }
    }
}
