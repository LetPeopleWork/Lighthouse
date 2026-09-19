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
    public sealed record SleRiskDto(string ReferenceId, int Risk);
}
