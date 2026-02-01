using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class DeliveryRuleService : IDeliveryRuleService
    {
        private const string EqualsOperator = "equals";

        private const string NotEqualsOperator = "notequals";

        private const string ContainsOperator = "contains";

        private const string FeatureTypeKey = "feature.type";

        private const string FeatureStateKey = "feature.state";

        private const string FeatureNameKey = "feature.name";

        private const string FeatureReferenceIdKey = "feature.referenceid";

        private const string FeatureParentReferenceIdKey = "feature.parentreferenceid";

        private const string FeatureTagsKey = "feature.tags";
        private const string AdditionalFieldBaseKey = "additionalField.";
        private static readonly HashSet<string> FixedFieldKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            FeatureTypeKey,
            FeatureStateKey,
            FeatureNameKey,
            FeatureReferenceIdKey,
            FeatureParentReferenceIdKey,
            FeatureTagsKey
        };

        private static readonly HashSet<string> ValidOperators = new(StringComparer.OrdinalIgnoreCase)
        {
            EqualsOperator,
            NotEqualsOperator,
            ContainsOperator
        };

        public DeliveryRuleSchema GetRuleSchema(Portfolio portfolio)
        {
            var fields = GetFixedFields();

            foreach (var additionalField in portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
            {
                fields.Add(new DeliveryRuleFieldDefinition
                {
                    FieldKey = $"additionalField.{additionalField.Id}",
                    DisplayName = additionalField.DisplayName,
                    IsMultiValue = false
                });
            }

            return new DeliveryRuleSchema
            {
                Fields = fields,
                Operators = [EqualsOperator, NotEqualsOperator, ContainsOperator],
                MaxRules = DeliveryRuleSet.MaxRules,
                MaxValueLength = DeliveryRuleSet.MaxValueLength
            };
        }

        public IEnumerable<Feature> GetMatchingFeaturesForRuleset(DeliveryRuleSet ruleSet, IEnumerable<Feature> features)
        {
            var hasError = RuleSetHasError(ruleSet);
            if (hasError)
            {
                return [];
            }

            var matchingFeatures = features.Where(f => FeatureMatchesAllConditions(f, ruleSet.Conditions)).ToList();
            return matchingFeatures;
        }

        public void RecomputeRuleBasedDeliveries(Portfolio portfolio, IEnumerable<Delivery> deliveries)
        {
            foreach (var delivery in deliveries.Where(d => d.SelectionMode == DeliverySelectionMode.RuleBased))
            {
                if (string.IsNullOrEmpty(delivery.RuleDefinitionJson))
                {
                    continue;
                }
                
                var ruleSet = JsonSerializer.Deserialize<DeliveryRuleSet>(delivery.RuleDefinitionJson);
                if (ruleSet == null)
                {
                    continue;
                }

                var features = GetMatchingFeaturesForRuleset(ruleSet, portfolio.Features);
                if (features.Any())
                {
                    delivery.Features.Clear();
                    delivery.Features.AddRange(features);
                }
            }
        }

        private static bool RuleSetHasError(DeliveryRuleSet ruleSet)
        {
            switch (ruleSet.Conditions.Count)
            {
                case 0:
                case > DeliveryRuleSet.MaxRules:
                    return true;
            }

            foreach (var condition in ruleSet.Conditions)
            {
                if (!IsValidFieldKey(condition.FieldKey))
                {
                    return true;
                }

                if (!ValidOperators.Contains(condition.Operator))
                {
                    return true;
                }

                if (condition.Value.Length > DeliveryRuleSet.MaxValueLength)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidFieldKey(string fieldKey)
        {
            if (FixedFieldKeys.Contains(fieldKey))
            {
                return true;
            }

            // Additional fields have the format: additionalField.{id}
            if (!fieldKey.StartsWith(AdditionalFieldBaseKey, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var idPart = fieldKey[AdditionalFieldBaseKey.Length..];
            return int.TryParse(idPart, out _);

        }

        private static bool FeatureMatchesAllConditions(Feature feature, List<DeliveryRuleCondition> conditions)
        {
            return conditions.All(c => EvaluateCondition(feature, c));
        }

        private static bool EvaluateCondition(Feature feature, DeliveryRuleCondition condition)
        {
            var fieldValue = GetFieldValue(feature, condition.FieldKey);
            var op = condition.Operator.ToLowerInvariant();

            if (condition.FieldKey.Equals(FeatureTagsKey, StringComparison.OrdinalIgnoreCase))
            {
                return EvaluateTagsCondition(feature.Tags, op, condition.Value);
            }

            return op switch
            {
                EqualsOperator => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                NotEqualsOperator => !string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                ContainsOperator => fieldValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static bool EvaluateTagsCondition(List<string> tags, string op, string value)
        {
            return op.ToLowerInvariant() switch
            {
                EqualsOperator => tags.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase)),
                NotEqualsOperator => !tags.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase)),
                ContainsOperator => tags.Any(t => t.Contains(value, StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        private static string GetFieldValue(Feature feature, string fieldKey)
        {
            if (!fieldKey.StartsWith(AdditionalFieldBaseKey, StringComparison.OrdinalIgnoreCase))
            {
                return fieldKey.ToLowerInvariant() switch
                {
                    FeatureTypeKey => feature.Type,
                    FeatureStateKey => feature.State,
                    FeatureNameKey => feature.Name,
                    FeatureReferenceIdKey => feature.ReferenceId,
                    FeatureParentReferenceIdKey => feature.ParentReferenceId,
                    _ => string.Empty
                };
            }

            var idPart = fieldKey[AdditionalFieldBaseKey.Length..];
            if (!int.TryParse(idPart, out var fieldId))
            {
                return fieldKey.ToLowerInvariant() switch
                {
                    FeatureTypeKey => feature.Type,
                    FeatureStateKey => feature.State,
                    FeatureNameKey => feature.Name,
                    FeatureReferenceIdKey => feature.ReferenceId,
                    FeatureParentReferenceIdKey => feature.ParentReferenceId,
                    _ => string.Empty
                };
            }

            if (feature.AdditionalFieldValues.TryGetValue(fieldId, out var value))
            {
                return value ?? string.Empty;
            }
            return string.Empty;

        }

        private static List<DeliveryRuleFieldDefinition> GetFixedFields()
        {
            return
            [
                new DeliveryRuleFieldDefinition { FieldKey = FeatureTypeKey, DisplayName = "Type", IsMultiValue = false },
                new DeliveryRuleFieldDefinition { FieldKey = FeatureStateKey, DisplayName = "State", IsMultiValue = false },
                new DeliveryRuleFieldDefinition { FieldKey = FeatureNameKey, DisplayName = "Name", IsMultiValue = false },
                new DeliveryRuleFieldDefinition { FieldKey = FeatureReferenceIdKey, DisplayName = "Reference ID", IsMultiValue = false },
                new DeliveryRuleFieldDefinition { FieldKey = FeatureParentReferenceIdKey, DisplayName = "Parent Reference ID", IsMultiValue = false },
                new DeliveryRuleFieldDefinition { FieldKey = FeatureTagsKey, DisplayName = "Tags", IsMultiValue = true }
            ];
        }
    }
}
