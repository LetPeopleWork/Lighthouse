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
    /// <see cref="GetEffectiveRuleSet"/> is the application-layer auto-migration backfill: when an owner
    /// has no <c>BlockedRuleSetJson</c> yet, the legacy <c>BlockedStates</c>/<c>BlockedTags</c> are
    /// synthesized (on read, idempotently) into the equivalent OR'd rule set.
    /// </summary>
    public interface IBlockedItemService
    {
        bool IsBlocked(WorkItem item, Team owner);

        bool IsBlocked(Feature item, Portfolio owner);

        WorkItemRuleSet GetEffectiveRuleSet(WorkTrackingSystemOptionsOwner owner);

        bool ValidateRuleSet(WorkItemRuleSet ruleSet, WorkTrackingSystemOptionsOwner owner);
    }
}
