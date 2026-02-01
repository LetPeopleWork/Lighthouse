namespace Lighthouse.Backend.Models.DeliveryRules
{
    public class DeliveryRuleSchema
    {
        public List<DeliveryRuleFieldDefinition> Fields { get; set; } = [];

        public List<string> Operators { get; set; } = ["equals", "notEquals", "contains"];

        public int MaxRules { get; set; } = DeliveryRuleSet.MaxRules;

        public int MaxValueLength { get; set; } = DeliveryRuleSet.MaxValueLength;
    }
}
