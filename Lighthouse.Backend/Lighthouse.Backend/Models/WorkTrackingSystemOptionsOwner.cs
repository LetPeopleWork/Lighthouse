namespace Lighthouse.Backend.Models
{
    public abstract class WorkTrackingSystemOptionsOwner : IWorkItemQueryOwner
    {
        public int Id { get; set; }

        public string WorkItemQuery { get; set; } = string.Empty;

        public abstract List<string> WorkItemTypes { get; set; }

        public List<string> ToDoStates { get; set; } = new List<string> { "New", "Proposed", "To Do" };

        public List<string> DoingStates { get; set; } = new List<string> { "Active", "Resolved", "In Progress", "Committed" };

        public List<string> DoneStates { get; set; } = new List<string> { "Done", "Closed" };

        public List<string> Tags { get; set; } = new List<string>();

        public IEnumerable<string> AllStates => OpenStates.Union(DoneStates);

        public IEnumerable<string> OpenStates => ToDoStates.Union(DoingStates);

        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection WorkTrackingSystemConnection { get; set; }

        public StateCategories MapStateToStateCategory(string state)
        {
            if (IsStateInList(state, ToDoStates))
            {
                return StateCategories.ToDo;
            }

            if (IsStateInList(state, DoingStates))
            {
                return StateCategories.Doing;
            }

            if (IsStateInList(state, DoneStates))
            {
                return StateCategories.Done;
            }

            return StateCategories.Unknown;
        }

        private static bool IsStateInList(string state, List<string> states)
        {
            return states.Any(s => string.Equals(s, state, StringComparison.OrdinalIgnoreCase));
        }
    }
}
