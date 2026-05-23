namespace Lighthouse.Backend.Models.WorkItemRules
{
    public class WorkItemRuleCondition
    {
        public string FieldKey { get; set; } = string.Empty;

        public string Operator { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
