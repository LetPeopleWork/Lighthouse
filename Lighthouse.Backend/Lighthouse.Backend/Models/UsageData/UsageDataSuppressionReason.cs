namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Why a batch was not forwarded. Closed on purpose: an outcome that does not appear here cannot
    /// be counted, and an uncounted suppression is one nobody can find out about afterwards.
    /// </summary>
    public enum UsageDataSuppressionReason
    {
        /// <summary>
        /// An administrator has stopped usage data for this whole instance. Named for who did it
        /// rather than for the value in the row: the setting is a veto, so it is switched <em>on</em>
        /// to produce this, and a reader of the day's tally needs to know a policy was applied
        /// rather than that something was left off.
        /// </summary>
        DisabledByAdministrator,

        /// <summary>
        /// Nobody behind this batch is agreeing right now - no token, an unknown one, a refusal, a
        /// withdrawal, or a browser last seen too long ago to still count.
        /// </summary>
        NoLiveConsent,

        /// <summary>The browser handed in more than its share of the allowance.</summary>
        RateLimited,

        /// <summary>The instance has already forwarded as much as a day is allowed to carry.</summary>
        BudgetExhausted,

        /// <summary>
        /// The question could not be answered at all - the database would not say. One of the two
        /// reasons here that mean the feature is broken rather than switched off, which is why it
        /// carries what went wrong with it to somewhere an operator sees without going looking.
        /// </summary>
        EvaluationFailed,

        /// <summary>
        /// The batch was allowed out and the collector could not be reached, or refused it. Counted
        /// apart from the reasons above because it is the only one where somebody agreed, the
        /// allowance had room, and the data still did not arrive - which is the feature being broken
        /// rather than the feature being careful.
        /// </summary>
        SendFailed,
    }
}
