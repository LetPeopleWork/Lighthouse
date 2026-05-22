using Lighthouse.Backend.Models.DeliveryRules;

namespace Lighthouse.Backend.Services.Interfaces.DeliveryRules
{
    /// <summary>
    /// Generic rule-engine port. <c>Match</c> returns the subset of <paramref name="items"/>
    /// that satisfy <paramref name="ruleSet"/>. The caller decides whether matched items are
    /// kept (Include semantics) or discarded (Exclude semantics) per ADR-013. <c>IsValid</c>
    /// validates the rule-set against the field schema supplied by an <see cref="IRuleFieldProvider{T}"/>.
    /// </summary>
    public interface IRuleEvaluator<T> where T : class
    {
        IEnumerable<T> Match(DeliveryRuleSet ruleSet, IEnumerable<T> items, IRuleFieldProvider<T> fieldProvider);

        bool IsValid(DeliveryRuleSet ruleSet, DeliveryRuleSchema schema);
    }
}
