using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Answers every read of a Team's work items, and of their transitions, the way the database would,
    /// from the items given. The items are read on each call, so a test may add to its list after the
    /// set-up and the next read still sees them.
    /// </summary>
    public static class WorkItemRepositoryDoubles
    {
        public static void AnswersTeamReadsFrom(this Mock<IWorkItemRepository> repository, IEnumerable<WorkItem> items)
        {
            repository
                .Setup(r => r.DoneItemsOfTeam(It.IsAny<int>()))
                .Returns((int teamId) => items.Where(i => i.TeamId == teamId && i.StateCategory == StateCategories.Done).ToList());
            repository
                .Setup(r => r.DoingOrDoneItemsOfTeam(It.IsAny<int>()))
                .Returns((int teamId) => items.Where(i => i.TeamId == teamId && (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.Done)).ToList());
            repository
                .Setup(r => r.ToDoOrDoingItemsOfTeam(It.IsAny<int>()))
                .Returns((int teamId) => items.Where(i => i.TeamId == teamId && (i.StateCategory == StateCategories.ToDo || i.StateCategory == StateCategories.Doing)).ToList());
            repository
                .Setup(r => r.CountToDoOrDoingItemsOfTeam(It.IsAny<int>()))
                .Returns((int teamId) => items.Count(i => i.TeamId == teamId && (i.StateCategory == StateCategories.ToDo || i.StateCategory == StateCategories.Doing)));
            repository
                .Setup(r => r.ItemsOfTeam(It.IsAny<int>()))
                .Returns((int teamId) => items.Where(i => i.TeamId == teamId).ToList());
        }

        public static void AnswersTransitionReadsFrom(this Mock<IWorkItemStateTransitionRepository> repository, IEnumerable<WorkItemStateTransition> transitions)
        {
            repository
                .Setup(r => r.TransitionsOfWorkItems(It.IsAny<IReadOnlyCollection<int>>()))
                .Returns((IReadOnlyCollection<int> workItemIds) => transitions.Where(t => workItemIds.Contains(t.WorkItemId)).ToList());
        }
    }
}
