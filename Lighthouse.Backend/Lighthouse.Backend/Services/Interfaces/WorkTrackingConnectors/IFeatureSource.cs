using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// Reading the Features of a Portfolio, and the parents of those Features, from the tracker a connection points at.
    /// Declared apart from the connector, because not every tracker has records a Portfolio could roll up over; a
    /// connector that cannot read them does not take this on.
    /// </summary>
    public interface IFeatureSource
    {
        Task<List<Feature>> GetFeaturesForPortfolio(Portfolio portfolio, CancellationToken cancellationToken);

        Task<List<Feature>> GetParentFeaturesDetails(Portfolio portfolio, IEnumerable<string> parentFeatureIds, CancellationToken cancellationToken);
    }
}
