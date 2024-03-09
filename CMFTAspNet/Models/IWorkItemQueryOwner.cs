using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Models
{
    public interface IWorkItemQueryOwner : IWorkTrackingSystemOptionsOwner, IEntity
    {
        string WorkItemQuery { get; set; }
    }
}
