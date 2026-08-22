namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// The total result of asking a remote what a bound source currently is. A closed set rather than a
    /// nullable or an exception, so a failed read can never be mistaken for a deleted source - which is
    /// the distinction the broken-source state rests on.
    /// </summary>
    public abstract record DeliverySourceResolution
    {
        private DeliverySourceResolution()
        {
        }

        /// <summary>The remote answered and the source is there, with a date.</summary>
        public sealed record Resolved(DeliverySourceSnapshot Snapshot) : DeliverySourceResolution;

        /// <summary>The remote answered, and the source is not there.</summary>
        public sealed record NotFound : DeliverySourceResolution;

        /// <summary>The source is there and carries no date, so nothing can be synced from it.</summary>
        public sealed record NoDate(string Name) : DeliverySourceResolution;

        /// <summary>
        /// The remote could not be asked. Transient by assumption: this must never raise the
        /// broken-source state, or every network blip reads as a deleted source.
        /// </summary>
        public sealed record Unavailable(DeliverySourceUnavailableReason Reason) : DeliverySourceResolution;
    }
}
