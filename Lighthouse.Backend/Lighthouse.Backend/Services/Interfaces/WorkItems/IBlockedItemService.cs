using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.Services.Interfaces.WorkItems
{
    /// <summary>
    /// Single, pure-function authority for "is this item blocked?" (ADR-067). The third Include
    /// consumer of the rule engine (after delivery rules and the forecast filter): an item that
    /// MATCHES the owner's blocked rule set IS blocked. The port is read-only — it exposes no write
    /// method, so the "the blocked check silently persisted something" bug class is non-representable.
    ///
    /// <see cref="GetEffectiveRuleSet"/> reads <c>BlockedRuleSetJson</c> exclusively: when an owner has
    /// never configured a blocking rule, it returns a default empty rule set (no legacy fallback).
    /// </summary>
    public interface IBlockedItemService
    {
        bool IsBlocked(WorkItem item, Team owner);

        bool IsBlocked(Feature item, Portfolio owner);

        WorkItemRuleSet GetEffectiveRuleSet(WorkTrackingSystemOptionsOwner owner);

        /// <summary>
        /// Serializes <see cref="GetEffectiveRuleSet"/> as camelCase JSON — the exact contract the
        /// frontend rule builder parses. Read endpoints MUST use this rather than a raw
        /// <c>JsonSerializer.Serialize</c>, whose PascalCase output the frontend cannot parse
        /// (it silently falls back to an empty rule set).
        /// </summary>
        string GetEffectiveRuleSetJson(WorkTrackingSystemOptionsOwner owner);

        bool ValidateRuleSet(WorkItemRuleSet ruleSet, WorkTrackingSystemOptionsOwner owner);
    }
}
