namespace Lighthouse.Backend.Models.WorkItemRules
{
    public class WorkItemRuleSet
    {
        public const int SchemaVersion = 1;

        public const int MaxRules = 20;

        public const int MaxValueLength = 500;

        public const string ModeAnd = "and";

        public const string ModeOr = "or";

        public int Version { get; set; } = SchemaVersion;

        public string Mode { get; set; } = ModeAnd;

        public List<WorkItemRuleCondition> Conditions { get; set; } = [];
    }
}
