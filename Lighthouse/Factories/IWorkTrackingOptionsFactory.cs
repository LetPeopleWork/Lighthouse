using Lighthouse.WorkTracking;

namespace Lighthouse.Factories
{
    public interface IWorkTrackingOptionsFactory
    {
        IEnumerable<WorkTrackingSystemOption<T>> CreateOptionsForWorkTrackingSystem<T>(WorkTrackingSystems workTrackingSystem) where T : class;
    }
}
