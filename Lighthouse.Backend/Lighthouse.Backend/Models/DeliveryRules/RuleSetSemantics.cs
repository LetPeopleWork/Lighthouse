namespace Lighthouse.Backend.Models.DeliveryRules
{
    /// <summary>
    /// Caller-side descriptor for how a <see cref="DeliveryRuleSet"/> evaluation result should
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
