namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// A rendered block bound for one remote source. The adapter is handed finished text rather than a
    /// Delivery, so composing the block stays testable without a connector.
    /// </summary>
    public sealed record DeliveryForecastPublication(string SourceReference, string BlockText);
}
