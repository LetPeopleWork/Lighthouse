using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;

namespace Lighthouse.Backend.Services.Implementation.WorkItemRules
{
    public class RuleEvaluator<T> : IRuleEvaluator<T> where T : class
    {
        private const string EqualsOperator = "equals";

        private const string NotEqualsOperator = "notequals";

        private const string ContainsOperator = "contains";

        private const string TagsFieldKey = "feature.tags";

        public IEnumerable<T> Match(WorkItemRuleSet ruleSet, IEnumerable<T> items, IRuleFieldProvider<T> fieldProvider)
        {
            if (RuleSetHasError(ruleSet, fieldProvider))
            {
                return [];
            }

            return items.Where(item => MatchesAllConditions(item, ruleSet.Conditions, fieldProvider)).ToList();
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
            return string.Equals(op, EqualsOperator, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(op, NotEqualsOperator, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(op, ContainsOperator, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesAllConditions(T item, List<WorkItemRuleCondition> conditions, IRuleFieldProvider<T> fieldProvider)
        {
            return conditions.All(c => EvaluateCondition(item, c, fieldProvider));
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
                EqualsOperator => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                NotEqualsOperator => !string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                ContainsOperator => fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }

        private static bool EvaluateTagsCondition(IReadOnlyList<string> tags, string op, string value)
        {
            return op switch
            {
                EqualsOperator => tags.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase)),
                NotEqualsOperator => !tags.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase)),
                ContainsOperator => tags.Any(t => t.Contains(value, StringComparison.OrdinalIgnoreCase)),
                _ => false,
            };
        }
    }
}
