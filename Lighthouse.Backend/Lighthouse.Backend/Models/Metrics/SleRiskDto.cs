namespace Lighthouse.Backend.Models.Metrics
{
    /// <summary>
    /// One in-flight item's chance of missing its team's target. Addressed by ReferenceId rather than
    /// by database id, because that is how write-back addresses an item in the tracker and how the
    /// dialog's rows already identify themselves.
    ///
    /// A null risk means the question has no answer for that item, and the item is still listed.
    /// ComparableItems says which kind of no-answer it is: zero means nothing the team finished ever
    /// ran this long, and anything below the calculator's minimum means too little did for a number
    /// to be worth trusting. A reader deserves to be told those apart.
    /// </summary>
    public sealed record SleRiskDto(string ReferenceId, int? Risk, int ComparableItems);
}
