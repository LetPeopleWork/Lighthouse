namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// A source a Delivery could bind to. The server decides selectability so a direct POST cannot bind
    /// what the picker greys out.
    /// </summary>
    public sealed record DeliverySourceOption(
        string Id,
        string Name,
        DateTime? Date,
        bool IsRetiredAtSource,
        bool IsReleasedAtSource,
        bool IsSelectable,
        SourceOptionBlockReason? BlockedBecause);
}
