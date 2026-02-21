using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
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
            DataRetrievalValue = workTrackingSystemOptionsOwner.DataRetrievalValue;
            WorkItemTypes = workTrackingSystemOptionsOwner.WorkItemTypes;
            ToDoStates = workTrackingSystemOptionsOwner.ToDoStates;
            DoingStates = workTrackingSystemOptionsOwner.DoingStates;
            DoneStates = workTrackingSystemOptionsOwner.DoneStates;
            Tags = workTrackingSystemOptionsOwner.Tags;
            WorkTrackingSystemConnectionId = workTrackingSystemOptionsOwner.WorkTrackingSystemConnectionId;
            ServiceLevelExpectationProbability = workTrackingSystemOptionsOwner.ServiceLevelExpectationProbability;
            ServiceLevelExpectationRange = workTrackingSystemOptionsOwner.ServiceLevelExpectationRange;
            SystemWIPLimit = workTrackingSystemOptionsOwner.SystemWIPLimit;
            BlockedStates = workTrackingSystemOptionsOwner.BlockedStates;
            BlockedTags = workTrackingSystemOptionsOwner.BlockedTags;
            ParentOverrideAdditionalFieldDefinitionId = workTrackingSystemOptionsOwner.ParentOverrideAdditionalFieldDefinitionId;
            ProcessBehaviourChartBaselineStartDate = workTrackingSystemOptionsOwner.ProcessBehaviourChartBaselineStartDate;
            ProcessBehaviourChartBaselineEndDate = workTrackingSystemOptionsOwner.ProcessBehaviourChartBaselineEndDate;
            EstimationAdditionalFieldDefinitionId = workTrackingSystemOptionsOwner.EstimationAdditionalFieldDefinitionId;
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Name { get; set; }

        public string DataRetrievalValue { get; set; }

        public List<string> WorkItemTypes { get; set; } = [];

        public List<string> ToDoStates { get; set; } = [];

        public List<string> DoingStates { get; set; } = [];

        public List<string> DoneStates { get; set; } = [];

        public List<string> Tags { get; set; } = [];

        public List<string> BlockedStates { get; set; } = [];

        public List<string> BlockedTags { get; set; } = [];

        [JsonRequired]
        public int ServiceLevelExpectationProbability { get; set; }

        [JsonRequired]
        public int ServiceLevelExpectationRange { get; set; }

        [JsonRequired]
        public int WorkTrackingSystemConnectionId { get; set; }

        [JsonRequired]
        public int SystemWIPLimit { get; set; }

        public int? ParentOverrideAdditionalFieldDefinitionId { get; set; }

        public DateTime? ProcessBehaviourChartBaselineStartDate { get; set; }

        public DateTime? ProcessBehaviourChartBaselineEndDate { get; set; }

        public int? EstimationAdditionalFieldDefinitionId { get; set; }
    }
}