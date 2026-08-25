using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Interfaces.DeliverySources
{
    /// <summary>
    /// Applies what the remote currently says to every source-bound Delivery of a Portfolio, on the
    /// refresh that already runs. A sibling of the rule service at the same seam rather than a branch
    /// inside it, because a source is not a rule.
    ///
    /// It takes the narrowed collection rather than any list of Deliveries, so a Delivery somebody
    /// retired cannot reach the pass by being passed to it - #5698 pins a closure snapshot on a
    /// retired Delivery, and a sync that wrote to one would un-pin a record the product promises does
    /// not move again.
    /// </summary>
    public interface IDeliverySourceSyncService
    {
        Task ResyncSourceBoundDeliveries(Portfolio portfolio, RecordableDeliveries deliveries);
    }
}
