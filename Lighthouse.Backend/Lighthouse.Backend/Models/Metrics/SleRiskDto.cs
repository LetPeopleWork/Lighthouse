namespace Lighthouse.Backend.Models.Metrics
{
    /// <summary>
    /// One in-flight item's chance of missing its team's target. Addressed by ReferenceId rather than
    /// by database id, because that is how write-back addresses an item in the tracker and how the
    /// dialog's rows already identify themselves.
    ///
    /// The risk is not nullable, and that is the contract rather than an oversight: every item this
    /// collection lists carries a number. An item with nothing to say about it is absent instead, so
    /// being listed at all is what says the item is in flight today on a team with a target.
    /// </summary>
    /// <param name="FinishedItemsStillOpenAtThisAge">
    /// How much of the team's finished work was still open at this item's age. It qualifies the risk
    /// without explaining it: a share taken over two finished items and one taken over forty read
    /// identically otherwise, and the first moves by fifty points when one more item closes.
    ///
    /// A fact about the history rather than about the derivation, which is what lets it be one
    /// non-nullable integer. An item past its target is told it is certain to miss without the
    /// history being consulted at all, so a field meaning "how many items this was computed over"
    /// would have to say "none" there - indistinguishable from a team whose history really does hold
    /// nothing that ran this long.
    /// </param>
    public sealed record SleRiskDto(string ReferenceId, int Risk, int FinishedItemsStillOpenAtThisAge);
}
