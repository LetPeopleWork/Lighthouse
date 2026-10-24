namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// Why a delivery source's whole list was read, so the line that reports the read's cost can say who paid for it:
    /// the picker's list, the preview sent on each pick, saving a Delivery bound to a source, or the regular refresh.
    /// </summary>
    public enum DeliverySourceSweepPurpose
    {
        Picker,
        Preview,
        Save,
        Refresh,
    }
}
