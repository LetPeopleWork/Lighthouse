using Lighthouse.Backend.Models;
using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Factories
{
    public interface IWorkTrackingSystemFactory
    {
        WorkTrackingSystemConnection CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem);
    }
}
