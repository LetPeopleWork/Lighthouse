using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// Reading the remote objects a Delivery can bind its date to. A capability of the connection rather
    /// than of the connector class, so one Jira connection may offer sources while another does not.
    /// </summary>
    public interface IDeliverySourceProvider
    {
        IReadOnlyList<DeliverySourceDescriptor> AvailableSources(WorkTrackingSystemConnection connection);

        /// <summary>
        /// Everything on this connection a Delivery could bind to, across every project the credential can
        /// see. Deliberately not narrowed to one project: a customer may coordinate its releases in a
        /// project of its own while the work itself lives in per-team projects, so the object carrying the
        /// date is routinely somewhere none of the Portfolio's own data points at.
        /// </summary>
        Task<IReadOnlyList<DeliverySourceOption>> GetOptions(WorkTrackingSystemConnection connection, string sourceKey);

        /// <summary>
        /// Resolves many bound references in one pass. Batched deliberately: the cost of a refresh must
        /// stay constant in the number of bound Deliveries rather than growing with it.
        /// </summary>
        Task<IReadOnlyDictionary<string, DeliverySourceResolution>> ResolveMany(
            WorkTrackingSystemConnection connection, string sourceKey, IReadOnlyList<string> sourceReferences);
    }
}
