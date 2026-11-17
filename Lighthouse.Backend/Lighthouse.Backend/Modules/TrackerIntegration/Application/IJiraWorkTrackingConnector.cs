using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IJiraWorkTrackingConnector : IWorkTrackingConnector, IBoardInformationProvider, IDeliverySourceProvider, IDeliveryForecastPublisher, IWriteBackTarget, IFeatureSource, IIncrementalSource
    {
    }
}
