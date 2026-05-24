namespace Lighthouse.Backend.Models.WorkItemRules
{
    /// <summary>
    /// Single source of truth for the operators understood by <see cref="WorkItemRuleSet"/>
    /// and surfaced through every schema endpoint. Add a new operator here, wire it into
    /// <see cref="Services.Implementation.WorkItemRules.RuleEvaluator{T}"/>, and every
    /// consumer of the schema (forecast filter, delivery rules, future surfaces) picks it
    /// up automatically.
    /// </summary>
    public static class RuleOperators
    {
#pragma warning disable CS0108
        public const string Equals = "equals";
#pragma warning restore CS0108

        public const string NotEquals = "notequals";

        public const string Contains = "contains";

        public const string NotContains = "notcontains";

        public const string IsEmpty = "isempty";

        public const string IsNotEmpty = "isnotempty";

        public static readonly IReadOnlyList<string> All = [
            Equals,
            NotEquals,
            Contains,
            NotContains,
            IsEmpty,
            IsNotEmpty,
        ];
    }
}
