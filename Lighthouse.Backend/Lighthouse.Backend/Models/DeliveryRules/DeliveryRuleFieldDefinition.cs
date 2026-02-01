namespace Lighthouse.Backend.Models.DeliveryRules
{
    public class DeliveryRuleFieldDefinition
    {
        public string FieldKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsMultiValue { get; set; } = false;
    }
}
