using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.DeliverySources
{
    /// <summary>
    /// Applies what the remote currently says to every source-bound Delivery of a Portfolio, on the
    /// refresh that already runs. A sibling of the rule service at the same seam rather than a branch
    /// inside it, because a source is not a rule.
    /// </summary>
    public interface IDeliverySourceSyncService
    {
        Task ResyncSourceBoundDeliveries(Portfolio portfolio, IReadOnlyList<Delivery> deliveries);
    }
}
