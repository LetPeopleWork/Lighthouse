using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Factories
{
    public interface IWorkTrackingOptionsFactory
    {
        IEnumerable<WorkTrackingSystemOption<T>> CreateOptionsForWorkTrackingSystem<T>(WorkTrackingSystems workTrackingSystem) where T : class;
    }
}
