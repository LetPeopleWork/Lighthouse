using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;

namespace Lighthouse.Backend.Services.Implementation.WorkItemRules
{
    public class WorkItemFieldProvider : IRuleFieldProvider<WorkItem>
    {
        private const string WorkItemTypeKey = "workitem.type";
        private const string WorkItemStateKey = "workitem.state";
        private const string WorkItemNameKey = "workitem.name";
        private const string WorkItemReferenceIdKey = "workitem.referenceid";
        private const string WorkItemParentReferenceIdKey = "workitem.parentreferenceid";
        private const string WorkItemTagsKey = "workitem.tags";
        private const string AdditionalFieldBaseKey = "additionalField.";

        private static readonly IReadOnlyList<WorkItemRuleFieldDefinition> FixedFields =
        [
            new() { FieldKey = WorkItemTypeKey, DisplayName = "Type", IsMultiValue = false },
            new() { FieldKey = WorkItemStateKey, DisplayName = "State", IsMultiValue = false },
            new() { FieldKey = WorkItemNameKey, DisplayName = "Name", IsMultiValue = false },
            new() { FieldKey = WorkItemReferenceIdKey, DisplayName = "Reference ID", IsMultiValue = false },
            new() { FieldKey = WorkItemParentReferenceIdKey, DisplayName = "Parent Reference ID", IsMultiValue = false },
            new() { FieldKey = WorkItemTagsKey, DisplayName = "Tags", IsMultiValue = true },
        ];

        public string GetFieldValue(WorkItem item, string fieldKey)
        {
            if (fieldKey.StartsWith(AdditionalFieldBaseKey, StringComparison.OrdinalIgnoreCase))
            {
                return GetAdditionalFieldValue(item, fieldKey);
            }

            return GetFixedFieldValue(item, fieldKey);
        }

        public IReadOnlyList<string> GetTagsForField(WorkItem item, string fieldKey)
        {
            if (!fieldKey.Equals(WorkItemTagsKey, StringComparison.OrdinalIgnoreCase))
            {
                return [];
            }

            return item.Tags;
        }

        public IReadOnlyList<WorkItemRuleFieldDefinition> GetFixedFields()
        {
            return FixedFields;
        }

        private static string GetFixedFieldValue(WorkItem item, string fieldKey)
        {
            return fieldKey.ToLowerInvariant() switch
            {
                WorkItemTypeKey => item.Type,
                WorkItemStateKey => item.State,
                WorkItemNameKey => item.Name,
                WorkItemReferenceIdKey => item.ReferenceId,
                WorkItemParentReferenceIdKey => item.ParentReferenceId,
                _ => string.Empty,
            };
        }

        private static string GetAdditionalFieldValue(WorkItem item, string fieldKey)
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
