namespace Lighthouse.Backend.Models.Metrics
{
    /// <summary>
    /// One in-flight item's chance of missing its team's target. Addressed by ReferenceId rather than
    /// by database id, because that is how write-back addresses an item in the tracker and how the
    /// dialog's rows already identify themselves.
    ///
    /// A null risk means the question has no answer for that item, and the item is still listed.
    /// </summary>
    public sealed record SleRiskDto(string ReferenceId, int? Risk);
}
