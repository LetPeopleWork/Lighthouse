using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IJiraWorkTrackingConnector : IWorkTrackingConnector, IBoardInformationProvider, IDeliverySourceProvider, IDeliveryForecastPublisher, IWriteBackTarget, IFeatureSource, IIncrementalSource
    {
    }
}
