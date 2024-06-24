using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Services.Factories
{
    public interface IWorkItemServiceFactory
    {
        IWorkItemService GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem);
    }
}