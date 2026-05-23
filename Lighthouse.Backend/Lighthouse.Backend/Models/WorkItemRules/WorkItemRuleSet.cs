namespace Lighthouse.Backend.Models.WorkItemRules
{
    public class WorkItemRuleSet
    {
        public const int SchemaVersion = 1;

        public const int MaxRules = 20;

        public const int MaxValueLength = 500;

        public int Version { get; set; } = SchemaVersion;

        public List<WorkItemRuleCondition> Conditions { get; set; } = [];
    }
}
