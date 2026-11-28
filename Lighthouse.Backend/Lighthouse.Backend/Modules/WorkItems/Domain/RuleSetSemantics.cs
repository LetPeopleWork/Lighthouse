using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.Modules.WorkItems.Domain
{
    /// <summary>
    /// Caller-side descriptor for how a <see cref="WorkItemRuleSet"/> evaluation result should
    /// be interpreted: <see cref="Include"/> keeps matched items (delivery-rules behaviour);
    /// <see cref="Exclude"/> discards them (forecast-throughput filter behaviour). Per ADR-013
    /// the evaluator itself is semantics-agnostic.
    /// </summary>
    public enum RuleSetSemantics
    {
        Include = 0,
        Exclude = 1,
    }
}
