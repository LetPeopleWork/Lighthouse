namespace Lighthouse.Backend.Models.WorkItemRules
{
    public class WorkItemRuleSchema
    {
        public List<WorkItemRuleFieldDefinition> Fields { get; set; } = [];

        public List<string> Operators { get; set; } = [.. RuleOperators.All];

        public int MaxRules { get; set; } = WorkItemRuleSet.MaxRules;

        public int MaxValueLength { get; set; } = WorkItemRuleSet.MaxValueLength;
    }
}
