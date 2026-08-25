namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// Why a bound source stopped resolving. Persisted as int - append only.
    /// </summary>
    public enum DeliverySourceUnavailableReason
    {
        /// <summary>The remote answered, and the source is not there. Permanent.</summary>
        SourceNotFound = 0,

        /// <summary>The source is there and carries no date, so nothing can be synced from it. Permanent.</summary>
        SourceHasNoDate = 1,

        /// <summary>
        /// The connection no longer offers this kind of source at all, so nothing here can ever resolve
        /// again. Permanent, and the only one of the three permanent reasons that is about the
        /// connection rather than about one object on it.
        /// </summary>
        CapabilityWithdrawn = 2,

        /// <summary>
        /// The remote could not be asked, or answered without mentioning this reference. Transient, and
        /// it must be kept apart from all three above: they say a binding is finished, this one says
        /// only that today's attempt told us nothing. Reading a blip as one of the others turns every
        /// unreachable minute into a Delivery that quietly stopped following its source.
        /// </summary>
        SourceReadFailed = 3,
    }
}
