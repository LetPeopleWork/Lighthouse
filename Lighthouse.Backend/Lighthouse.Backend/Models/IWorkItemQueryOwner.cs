using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public interface IWorkItemQueryOwner : IWorkTrackingSystemOptionsOwner, IEntity
    {
        string Name { get; set; }

        string DataRetrievalValue { get; set; }

        List<string> WorkItemTypes { get; set; }

        List<string> ToDoStates { get; set; }

        List<string> DoingStates { get; set; }

        List<string> DoneStates { get; set; }

        List<string> Tags { get; set; }

        IEnumerable<string> AllStates { get; }

        IEnumerable<string> OpenStates { get; }

        List<string> BlockedStates { get; set; }
        
        List<string> BlockedTags { get; set; }

        int DoneItemsCutoffDays { get; set; }

        StateCategories MapStateToStateCategory(string state);
    }
}
