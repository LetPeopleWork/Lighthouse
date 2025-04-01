namespace Lighthouse.Backend.Models
{
    public class WorkTrackingSystemOptionsOwner : IWorkItemQueryOwner
    {
        public int Id { get; set; }

        public string WorkItemQuery { get; set; } = string.Empty;

        public List<string> ToDoStates { get; set; } = new List<string> { "New", "Proposed", "To Do" };

        public List<string> DoingStates { get; set; } = new List<string> { "Active", "Resolved", "In Progress", "Committed" };

        public List<string> DoneStates { get; set; } = new List<string> { "Done", "Closed" };

        public IEnumerable<string> AllStates => OpenStates.Union(DoneStates);

        public IEnumerable<string> OpenStates => ToDoStates.Union(DoingStates);

        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection WorkTrackingSystemConnection { get; set; }

        public StateCategories MapStateToStateCategory(string state)
        {
            if (ToDoStates.Contains(state))
            {
                return StateCategories.ToDo;
            }

            if (DoingStates.Contains(state))
            {
                return StateCategories.Doing;
            }

            if (DoneStates.Contains(state))
            {
                return StateCategories.Done;
            }

            return StateCategories.Unknown;
        }
    }
}
