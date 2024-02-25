using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Factories
{
    public interface IWorkTrackingOptionsFactory
    {
        IEnumerable<WorkTrackingSystemOption> CreateOptionsForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem);
    }
}
