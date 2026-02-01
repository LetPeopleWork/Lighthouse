namespace Lighthouse.Backend.Models.DeliveryRules
{
    public class DeliveryRuleCondition
    {
        public string FieldKey { get; set; } = string.Empty;

        public string Operator { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
