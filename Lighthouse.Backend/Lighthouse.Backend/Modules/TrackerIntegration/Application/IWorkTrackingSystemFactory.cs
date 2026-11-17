using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IWorkTrackingSystemFactory
    {
        WorkTrackingSystemConnection CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem);
    }
}
