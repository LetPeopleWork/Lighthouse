using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IWorkItemRepository : IRepository<WorkItem>
    {
        void RemoveWorkItemsForTeam(int teamId);

        /// <summary>The Team's work items that are Done, each with its Team, tracked.</summary>
        IReadOnlyList<WorkItem> DoneItemsOfTeam(int teamId);

        /// <summary>The Team's work items that are Doing or Done, each with its Team, tracked.</summary>
        IReadOnlyList<WorkItem> DoingOrDoneItemsOfTeam(int teamId);

        /// <summary>The Team's work items that are To Do or Doing, each with its Team, tracked.</summary>
        IReadOnlyList<WorkItem> ToDoOrDoingItemsOfTeam(int teamId);

        /// <summary>How many of the Team's work items are To Do or Doing, counted by the database.</summary>
        int CountToDoOrDoingItemsOfTeam(int teamId);

        /// <summary>Every work item of the Team, whatever its state, each with its Team, tracked.</summary>
        IReadOnlyList<WorkItem> ItemsOfTeam(int teamId);
    }
}
