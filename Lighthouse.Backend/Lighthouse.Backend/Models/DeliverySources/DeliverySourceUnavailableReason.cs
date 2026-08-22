namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// Why a bound source stopped resolving. Persisted as int - append only.
    /// </summary>
    public enum DeliverySourceUnavailableReason
    {
        SourceNotFound = 0,
        SourceHasNoDate = 1,
        CapabilityWithdrawn = 2,
    }
}
