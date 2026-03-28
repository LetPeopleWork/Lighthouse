using Lighthouse.Backend.Extensions;

namespace Lighthouse.Backend.Models
{
    public abstract class WorkTrackingSystemOptionsOwner : IWorkItemQueryOwner
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string DataRetrievalValue { get; set; } = string.Empty;

        public DateTime UpdateTime { get; set; }

        public abstract List<string> WorkItemTypes { get; set; }

        public List<string> ToDoStates { get; set; } = new List<string> { "New", "Proposed", "To Do" };

        public List<string> DoingStates { get; set; } = new List<string> { "Active", "Resolved", "In Progress", "Committed" };

        public List<string> DoneStates { get; set; } = new List<string> { "Done", "Closed" };

        public List<string> Tags { get; set; } = new List<string>();

        public IEnumerable<string> AllStates => GetRawStatesForCategory(ToDoStates)
            .Union(GetRawStatesForCategory(DoingStates))
            .Union(GetRawStatesForCategory(DoneStates));

        public IEnumerable<string> OpenStates => GetRawStatesForCategory(ToDoStates)
            .Union(GetRawStatesForCategory(DoingStates));

        public int ServiceLevelExpectationProbability { get; set; } = 0;

        public int ServiceLevelExpectationRange { get; set; } = 0;

        public int SystemWIPLimit { get; set; } = 0;

        public int WorkTrackingSystemConnectionId { get; set; }

        public List<string> BlockedStates { get; set; } = [];

        public List<string> BlockedTags { get; set; } = [];

        public List<StateMapping> StateMappings { get; set; } = [];

        public abstract int DoneItemsCutoffDays { get; set; }

        public DateTime? ProcessBehaviourChartBaselineStartDate { get; set; }

        public DateTime? ProcessBehaviourChartBaselineEndDate { get; set; }

        public WorkTrackingSystemConnection WorkTrackingSystemConnection { get; set; }

        public int? EstimationAdditionalFieldDefinitionId { get; set; }

        public string? EstimationUnit { get; set; }

        public bool UseNonNumericEstimation { get; set; }

        public List<string> EstimationCategoryValues { get; set; } = [];

        public int? ParentOverrideAdditionalFieldDefinitionId { get; set; }

        public StateCategories MapStateToStateCategory(string state)
        {
            if (GetRawStatesForCategory(ToDoStates).IsItemInList(state))
            {
                return StateCategories.ToDo;
            }

            if (GetRawStatesForCategory(DoingStates).IsItemInList(state))
            {
                return StateCategories.Doing;
            }

            if (GetRawStatesForCategory(DoneStates).IsItemInList(state))
            {
                return StateCategories.Done;
            }

            return StateCategories.Unknown;
        }

        public List<string> GetRawStatesForCategory(List<string> categoryStates)
        {
            var rawStates = new List<string>();

            foreach (var entry in categoryStates)
            {
                var mapping = StateMappings.FirstOrDefault(m =>
                    string.Equals(m.Name, entry, StringComparison.OrdinalIgnoreCase));

                if (mapping != null)
                {
                    rawStates.AddRange(mapping.States);
                }
                else
                {
                    rawStates.Add(entry);
                }
            }

            return rawStates;
        }

        public string MapRawStateToMappedName(string rawState)
        {
            var mapping = StateMappings.FirstOrDefault(m =>
                m.States.Any(s => string.Equals(s, rawState, StringComparison.OrdinalIgnoreCase)));

            return mapping?.Name ?? rawState;
        }

        public void ResetUpdateTime()
        {
            UpdateTime = DateTime.MinValue;
        }

        public void RefreshUpdateTime()
        {
            UpdateTime = DateTime.UtcNow;
        }
    }
}
