using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IWorkTrackingConnectorFactory
    {
        IWorkTrackingConnector GetWorkTrackingConnector(WorkTrackingSystems workTrackingSystem);
    }
}