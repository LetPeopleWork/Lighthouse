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

    /// <summary>
    /// One band of the aging chart's risk background: the first age at which an item's chance of
    /// missing the target reaches Risk. The band runs from there up to the next one.
    ///
    /// A band the history cannot place is absent rather than guessed - and because the evidence
    /// thins as the age grows, the bands that go missing are the worst ones.
    /// </summary>
    public sealed record SleRiskZoneDto(int Risk, int FromAge);
}
