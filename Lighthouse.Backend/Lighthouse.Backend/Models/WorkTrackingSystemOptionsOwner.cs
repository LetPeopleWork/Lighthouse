namespace Lighthouse.Backend.Models
{
    public abstract class WorkTrackingSystemOptionsOwner : IWorkItemQueryOwner
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string WorkItemQuery { get; set; } = string.Empty;

        public DateTime UpdateTime { get; set; }

        public abstract List<string> WorkItemTypes { get; set; }

        public List<string> ToDoStates { get; set; } = new List<string> { "New", "Proposed", "To Do" };

        public List<string> DoingStates { get; set; } = new List<string> { "Active", "Resolved", "In Progress", "Committed" };

        public List<string> DoneStates { get; set; } = new List<string> { "Done", "Closed" };

        public List<string> Tags { get; set; } = new List<string>();

        public IEnumerable<string> AllStates => OpenStates.Union(DoneStates);

        public IEnumerable<string> OpenStates => ToDoStates.Union(DoingStates);

        public int ServiceLevelExpectationProbability { get; set; } = 0;

        public int ServiceLevelExpectationRange { get; set; } = 0;

        public int SystemWIPLimit { get; set; } = 0;

        public string? ParentOverrideField { get; set; } = string.Empty;

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

        public void ResetUpdateTime()
        {
            UpdateTime = DateTime.MinValue;
        }

        public void RefreshUpdateTime()
        {
            UpdateTime = DateTime.UtcNow;
        }

        private static bool IsStateInList(string state, List<string> states)
        {
            return states.Any(s => string.Equals(s, state, StringComparison.OrdinalIgnoreCase));
        }
    }
}
