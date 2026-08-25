namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// A rendered block bound for one remote source. The adapter is handed finished text rather than a
    /// Delivery, so composing the block stays testable without a connector.
    ///
    /// The source key travels with the reference for the same reason it does on the way in: a
    /// connection that one day offers two kinds of remote delivery object would otherwise be handed an
    /// id with nothing saying which kind of thing it names.
    /// </summary>
    public sealed record DeliveryForecastPublication(string SourceKey, string SourceReference, string BlockText);
}
