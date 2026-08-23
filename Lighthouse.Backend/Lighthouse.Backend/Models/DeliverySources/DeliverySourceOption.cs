namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// The project a source lives in. Carried alongside the source because two projects on one connection
    /// routinely name a Release the same thing - a picker showing bare names would offer the reader two
    /// identical rows and no way to tell which one they were binding to.
    /// </summary>
    public sealed record DeliverySourceProject(string Key, string Name);

    /// <summary>
    /// A source a Delivery could bind to. The server decides selectability so a direct POST cannot bind
    /// what the picker greys out.
    /// </summary>
    public sealed record DeliverySourceOption(
        string Id,
        string Name,
        DateTime? Date,
        DeliverySourceProject Project,
        bool IsRetiredAtSource,
        bool IsReleasedAtSource,
        SourceOptionBlockReason? BlockedBecause)
    {
        public bool IsSelectable => BlockedBecause is null;
    }
}
