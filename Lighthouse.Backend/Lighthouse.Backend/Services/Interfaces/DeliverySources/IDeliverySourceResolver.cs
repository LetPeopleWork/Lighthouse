using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Services.Interfaces.DeliverySources
{
    /// <summary>
    /// Turns a set of bound source references into verdicts, and intersects the remote membership with
    /// the Features the Portfolio actually tracks.
    ///
    /// One resolver with two callers - create and the refresh re-sync - so the two can never drift on
    /// which verdicts are recoverable. Batched in one call per pass, because the cost of a refresh has
    /// to stay constant in the number of bound Deliveries.
    /// </summary>
    public interface IDeliverySourceResolver
    {
        Task<IReadOnlyDictionary<string, DeliverySourceResolution>> ResolveForPortfolio(
            Portfolio portfolio, string sourceKey, IReadOnlyList<string> sourceReferences);
    }
}
