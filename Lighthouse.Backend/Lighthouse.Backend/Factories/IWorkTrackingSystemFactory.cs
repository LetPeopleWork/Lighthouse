using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Factories
{
    public interface IWorkTrackingSystemFactory
    {
        WorkTrackingSystemConnection CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem);
    }
}
