using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IAzureDevOpsWorkTrackingConnector : IWorkTrackingConnector, IBoardInformationProvider, IWriteBackTarget, IFeatureSource, IIncrementalSource
    {
    }
}