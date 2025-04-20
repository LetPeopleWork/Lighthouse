using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Factories
{
    public interface IWorkTrackingConnectorFactory
    {
        IWorkTrackingConnector GetWorkTrackingConnector(WorkTrackingSystems workTrackingSystem);
    }
}