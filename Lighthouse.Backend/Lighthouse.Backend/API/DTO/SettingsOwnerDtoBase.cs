using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public abstract class SettingsOwnerDtoBase
    {
        protected SettingsOwnerDtoBase()
        {
        }

        protected SettingsOwnerDtoBase(WorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            Id = workTrackingSystemOptionsOwner.Id;
            Name = workTrackingSystemOptionsOwner.Name;
            WorkItemQuery = workTrackingSystemOptionsOwner.WorkItemQuery;
            WorkItemTypes = workTrackingSystemOptionsOwner.WorkItemTypes;
            ToDoStates = workTrackingSystemOptionsOwner.ToDoStates;
            DoingStates = workTrackingSystemOptionsOwner.DoingStates;
            DoneStates = workTrackingSystemOptionsOwner.DoneStates;
            Tags = workTrackingSystemOptionsOwner.Tags;
            WorkTrackingSystemConnectionId = workTrackingSystemOptionsOwner.WorkTrackingSystemConnectionId;
            ServiceLevelExpectationProbability = workTrackingSystemOptionsOwner.ServiceLevelExpectationProbability;
            ServiceLevelExpectationRange = workTrackingSystemOptionsOwner.ServiceLevelExpectationRange;
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Name { get; set; }

        public string WorkItemQuery { get; set; }

        public List<string> WorkItemTypes { get; set; } = [];

        public List<string> ToDoStates { get; set; } = [];

        public List<string> DoingStates { get; set; } = [];

        public List<string> DoneStates { get; set; } = [];

        public List<string> Tags { get; set; } = [];
        
        [JsonRequired]
        public int ServiceLevelExpectationProbability { get; set; }

        [JsonRequired]
        public int ServiceLevelExpectationRange { get; set; }

        [JsonRequired]
        public int WorkTrackingSystemConnectionId { get; set; }
    }
}