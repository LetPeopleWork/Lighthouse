using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Factories
{
    public interface IWorkTrackingOptionsFactory
    {
        IEnumerable<WorkTrackingSystemOption<T>> CreateOptionsForWorkTrackingSystem<T>(WorkTrackingSystems workTrackingSystem) where T : class;
    }
}
