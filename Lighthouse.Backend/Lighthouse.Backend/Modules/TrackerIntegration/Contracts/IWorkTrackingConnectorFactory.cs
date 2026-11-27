using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Contracts
{
    public interface IWorkTrackingConnectorFactory
    {
        IWorkTrackingConnector GetWorkTrackingConnector(WorkTrackingSystems workTrackingSystem);
    }
}