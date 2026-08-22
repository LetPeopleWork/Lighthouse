namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// One selection mode a connection offers beyond Manual and Rule-based. The key is what a create
    /// payload names; the display name is what the tab shows.
    /// </summary>
    public sealed record DeliverySourceDescriptor(string Key, string DisplayName);
}
