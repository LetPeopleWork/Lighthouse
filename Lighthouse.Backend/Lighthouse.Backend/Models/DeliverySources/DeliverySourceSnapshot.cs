namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// What the remote says a source currently is. Members cross the port as reference ids rather than
    /// Features, so an adapter cannot hand back domain objects.
    /// </summary>
    public sealed record DeliverySourceSnapshot(string Name, DateTime Date, IReadOnlyList<string> MemberReferenceIds);
}
