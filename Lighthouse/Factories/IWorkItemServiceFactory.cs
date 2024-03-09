using Lighthouse.Models;
using Lighthouse.Services.Interfaces;
using Lighthouse.WorkTracking;

namespace Lighthouse.Services.Factories
{
    public interface IWorkItemServiceFactory
    {
        IWorkItemService GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem);
    }
}