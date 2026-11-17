using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Modules.Promises.Domain;

namespace Lighthouse.Backend.Modules.Promises.Application
{
    /// <summary>
    /// Composes the published block. The same class merges it into a description through Tracker integration's
    /// description block merger, so the markers that compose the block and find it again stay in one place.
    /// Pure - no I/O - so every marker rule is a unit test rather than an integration test.
    /// </summary>
    public interface IDeliveryForecastBlockRenderer
    {
        /// <summary>
        /// The block as it is written into the remote description. Composing it and finding it again
        /// live together on purpose: matched apart, the two can drift into a state where every write
        /// appends and the description fills up with forecasts.
        /// </summary>
        string Render(DeliveryForecastBlock block);
    }
}
