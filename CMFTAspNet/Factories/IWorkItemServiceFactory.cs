using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Services.Factories
{
    public interface IWorkItemServiceFactory
    {
        IWorkItemService GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem);
    }
}