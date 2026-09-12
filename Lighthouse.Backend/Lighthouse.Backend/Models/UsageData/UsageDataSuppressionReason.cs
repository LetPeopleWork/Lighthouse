namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Why a batch was not forwarded. Closed on purpose: an outcome that does not appear here cannot
    /// be counted, and an uncounted suppression is one nobody can find out about afterwards.
    /// </summary>
    public enum UsageDataSuppressionReason
    {
        /// <summary>The operator switched the whole feature off.</summary>
        MasterSwitchOff,

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
        /// The question could not be answered at all. The only reason here that means the feature is
        /// broken rather than switched off, which is why it is the one an operator has to be able to
        /// find.
        /// </summary>
        EvaluationFailed,
    }
}
