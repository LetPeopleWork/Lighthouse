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

        Task<IReadOnlyList<DeliverySourceOption>> GetOptions(WorkTrackingSystemConnection connection, string sourceKey, string projectReference);

        /// <summary>
        /// Resolves many bound references in one pass. Batched deliberately: the cost of a refresh must
        /// stay constant in the number of bound Deliveries rather than growing with it.
        /// </summary>
        Task<IReadOnlyDictionary<string, DeliverySourceResolution>> ResolveMany(
            WorkTrackingSystemConnection connection, string sourceKey, IReadOnlyList<string> sourceReferences);
    }
}
