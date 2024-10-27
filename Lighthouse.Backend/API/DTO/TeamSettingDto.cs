using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class TeamSettingDto
    {
        public TeamSettingDto()
        {            
        }

        public TeamSettingDto(Team team)
        {
            Id = team.Id;    
            Name = team.Name;
            ThroughputHistory = team.ThroughputHistory;
            FeatureWIP = team.FeatureWIP;
            WorkItemQuery = team.WorkItemQuery;
            WorkItemTypes = team.WorkItemTypes;
            WorkTrackingSystemConnectionId = team.WorkTrackingSystemConnectionId;
            RelationCustomField = team.AdditionalRelatedField;
            ToDoStates = team.ToDoStates;
            DoingStates = team.DoingStates;
            DoneStates = team.DoneStates;
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Name { get; set; }

        [JsonRequired]
        public int ThroughputHistory { get; set; }

        [JsonRequired]
        public int FeatureWIP { get; set; }

        public string WorkItemQuery { get; set; }

        public List<string> WorkItemTypes { get; set; }
        
        public List<string> ToDoStates { get; set; } = [];

        public List<string> DoingStates { get; set; } = [];

        public List<string> DoneStates { get; set; } = [];

        [JsonRequired]
        public int WorkTrackingSystemConnectionId { get; set; }

        public string RelationCustomField { get; set; }
    }
}
