using Lighthouse.Services.Interfaces;

namespace Lighthouse.Models
{
    public interface IWorkItemQueryOwner : IWorkTrackingSystemOptionsOwner, IEntity
    {
        string WorkItemQuery { get; set; }
    }
}
