using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface ILinearWorkTrackingConnector : IWorkTrackingConnector, IBoardInformationProvider, IFeatureSource
    {
    }
}
