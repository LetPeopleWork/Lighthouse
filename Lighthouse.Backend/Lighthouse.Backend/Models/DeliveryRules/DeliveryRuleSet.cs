namespace Lighthouse.Backend.Models.DeliveryRules
{
    public class DeliveryRuleSet
    {
        public const int SchemaVersion = 1;

        public const int MaxRules = 20;

        public const int MaxValueLength = 500;

        public int Version { get; set; } = SchemaVersion;

        public List<DeliveryRuleCondition> Conditions { get; set; } = [];
    }
}
