using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Interfaces.DeliveryRules;

namespace Lighthouse.Backend.Services.Implementation.DeliveryRules
{
    public class FeatureFieldProvider : IRuleFieldProvider<Feature>
    {
        private const string FeatureTypeKey = "feature.type";
        private const string FeatureStateKey = "feature.state";
        private const string FeatureNameKey = "feature.name";
        private const string FeatureReferenceIdKey = "feature.referenceid";
        private const string FeatureParentReferenceIdKey = "feature.parentreferenceid";
        private const string FeatureTagsKey = "feature.tags";
        private const string AdditionalFieldBaseKey = "additionalField.";

        private static readonly IReadOnlyList<DeliveryRuleFieldDefinition> FixedFields =
        [
            new() { FieldKey = FeatureTypeKey, DisplayName = "Type", IsMultiValue = false },
            new() { FieldKey = FeatureStateKey, DisplayName = "State", IsMultiValue = false },
            new() { FieldKey = FeatureNameKey, DisplayName = "Name", IsMultiValue = false },
            new() { FieldKey = FeatureReferenceIdKey, DisplayName = "Reference ID", IsMultiValue = false },
            new() { FieldKey = FeatureParentReferenceIdKey, DisplayName = "Parent Reference ID", IsMultiValue = false },
            new() { FieldKey = FeatureTagsKey, DisplayName = "Tags", IsMultiValue = true },
        ];

        public string GetFieldValue(Feature item, string fieldKey)
        {
            if (fieldKey.StartsWith(AdditionalFieldBaseKey, StringComparison.OrdinalIgnoreCase))
            {
                return GetAdditionalFieldValue(item, fieldKey);
            }

            return GetFixedFieldValue(item, fieldKey);
        }

        public IReadOnlyList<string> GetTagsForField(Feature item, string fieldKey)
        {
            if (!fieldKey.Equals(FeatureTagsKey, StringComparison.OrdinalIgnoreCase))
            {
                return [];
            }

            return item.Tags;
        }

        public IReadOnlyList<DeliveryRuleFieldDefinition> GetFixedFields()
        {
            return FixedFields;
        }

        private static string GetFixedFieldValue(Feature item, string fieldKey)
        {
            return fieldKey.ToLowerInvariant() switch
            {
                FeatureTypeKey => item.Type,
                FeatureStateKey => item.State,
                FeatureNameKey => item.Name,
                FeatureReferenceIdKey => item.ReferenceId,
                FeatureParentReferenceIdKey => item.ParentReferenceId,
                _ => string.Empty,
            };
        }

        private static string GetAdditionalFieldValue(Feature item, string fieldKey)
        {
            var idPart = fieldKey[AdditionalFieldBaseKey.Length..];
            if (!int.TryParse(idPart, out var fieldId))
            {
                return string.Empty;
            }

            if (item.AdditionalFieldValues.TryGetValue(fieldId, out var value))
            {
                return value ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
