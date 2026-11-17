using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IWorkTrackingConnectorFactory
    {
        IWorkTrackingConnector GetWorkTrackingConnector(WorkTrackingSystems workTrackingSystem);
    }
}