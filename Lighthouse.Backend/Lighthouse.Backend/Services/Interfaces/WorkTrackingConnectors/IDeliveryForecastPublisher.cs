using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// Writing a forecast back onto the remote source a Delivery is bound to. Declared separately from
    /// reading, because a connection can be allowed to read sources and refused the write - which is the
    /// state the refusal report exists to surface.
    /// </summary>
    public interface IDeliveryForecastPublisher
    {
        bool SupportsDeliveryForecastPublishing(WorkTrackingSystemConnection connection);

        Task<DeliveryForecastPublishResult> PublishAsync(
            WorkTrackingSystemConnection connection, DeliveryForecastPublication publication);
    }
}
