using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public interface IWorkItemQueryOwner : IWorkTrackingSystemOptionsOwner, IEntity
    {
        string WorkItemQuery { get; set; }

        List<string> WorkItemTypes { get; set; }

        List<string> ToDoStates { get; set; }

        List<string> DoingStates { get; set; }

        List<string> DoneStates { get; set; }

        List<string> Tags { get; set; }

        IEnumerable<string> AllStates { get; }

        IEnumerable<string> OpenStates { get; }

        StateCategories MapStateToStateCategory(string state);
    }
}
