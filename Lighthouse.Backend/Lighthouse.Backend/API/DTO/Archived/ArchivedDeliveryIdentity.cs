namespace Lighthouse.Backend.API.DTO.Archived
{
    /// <summary>
    /// The few things about a retired Delivery that are still read from the Delivery itself - what it
    /// is called, when it was aimed at, which Portfolio it belongs to. Everything a reader sees
    /// beyond this comes from what was written down on the day it closed.
    /// </summary>
    public sealed record ArchivedDeliveryIdentity(int Id, string Name, DateTime Date, int PortfolioId, Guid ConcurrencyToken);
}
