namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// Why an offered source cannot be bound. Two members rather than one bool, because they send the
    /// reader somewhere different: one is fixed by setting a date, the other by picking another source.
    /// Persisted as int - append only.
    /// </summary>
    public enum SourceOptionBlockReason
    {
        NoDateSet = 0,
        RetiredAtSource = 1,
    }
}
