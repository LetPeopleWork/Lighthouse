using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public interface IWorkItemQueryOwner : IWorkTrackingSystemOptionsOwner, IEntity
    {
        string WorkItemQuery { get; set; }
    }
}
