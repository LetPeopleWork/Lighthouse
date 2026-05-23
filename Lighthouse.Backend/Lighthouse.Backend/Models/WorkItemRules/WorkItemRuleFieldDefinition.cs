namespace Lighthouse.Backend.Models.WorkItemRules
{
    public class WorkItemRuleFieldDefinition
    {
        public string FieldKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsMultiValue { get; set; } = false;
    }
}
