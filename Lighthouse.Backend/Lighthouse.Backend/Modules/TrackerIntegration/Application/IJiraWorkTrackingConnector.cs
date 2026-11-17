using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IJiraWorkTrackingConnector : IWorkTrackingConnector, IBoardInformationProvider, IDeliverySourceProvider, IDeliveryForecastPublisher, IWriteBackTarget, IFeatureSource, IIncrementalSource
    {
    }
}
