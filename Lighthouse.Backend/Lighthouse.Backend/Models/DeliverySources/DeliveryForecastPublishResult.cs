namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// The total result of publishing a block. Refusal and a missing target are separate members because
    /// they send an administrator to fix entirely different things.
    /// </summary>
    public abstract record DeliveryForecastPublishResult
    {
        private DeliveryForecastPublishResult()
        {
        }

        public sealed record Published : DeliveryForecastPublishResult;

        /// <summary>
        /// The remote refused the write. The reason carries the remote's own sentence, which already
        /// names what to fix in the reader's vocabulary.
        /// </summary>
        public sealed record Refused(string Reason) : DeliveryForecastPublishResult;

        /// <summary>The source no longer exists. Raises the broken-source state, not a refusal.</summary>
        public sealed record TargetMissing : DeliveryForecastPublishResult;
    }
}
