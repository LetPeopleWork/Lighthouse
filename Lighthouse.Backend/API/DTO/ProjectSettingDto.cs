using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class ProjectSettingDto
    {
        public ProjectSettingDto()
        {            
        }

        public ProjectSettingDto(Project project)
        {
            Id = project.Id;
            Name = project.Name;
            WorkItemTypes = project.WorkItemTypes;
            Milestones.AddRange(project.Milestones.Select(m => new MilestoneDto(m)));
            WorkItemQuery = project.WorkItemQuery;
            UnparentedItemsQuery = project.UnparentedItemsQuery;

            UsePercentileToCalculateDefaultAmountOfWorkItems = project.UsePercentileToCalculateDefaultAmountOfWorkItems;
            DefaultAmountOfWorkItemsPerFeature = project.DefaultAmountOfWorkItemsPerFeature;
            DefaultWorkItemPercentile = project.DefaultWorkItemPercentile;
            HistoricalFeaturesWorkItemQuery = project.HistoricalFeaturesWorkItemQuery;
            SizeEstimateField = project.SizeEstimateField;
            OverrideRealChildCountStates = project.OverrideRealChildCountStates;

            WorkTrackingSystemConnectionId = project.WorkTrackingSystemConnectionId;

            ToDoStates = project.ToDoStates;
            DoingStates = project.DoingStates;
            DoneStates = project.DoneStates;

            InvolvedTeams.AddRange(project.CreateInvolvedTeamDtos());
        }

        [JsonRequired]
        public int Id { get; set; } 

        public string Name { get; set; }

        public List<string> WorkItemTypes { get; set; } = [];

        public List<MilestoneDto> Milestones { get; set; } = [];

        public List<string> ToDoStates { get; set; } = [];

        public List<string> DoingStates { get; set; } = [];

        public List<string> DoneStates { get; set; } = [];

        public List<string> OverrideRealChildCountStates { get; set; } = [];

        public string WorkItemQuery { get; set; }

        public string UnparentedItemsQuery { get; set; }

        public bool UsePercentileToCalculateDefaultAmountOfWorkItems { get; set; }

        public int DefaultWorkItemPercentile { get; set; }

        public string HistoricalFeaturesWorkItemQuery { get; set; }

        [JsonRequired]
        public int DefaultAmountOfWorkItemsPerFeature { get; set; }

        [JsonRequired]
        public int WorkTrackingSystemConnectionId { get; set; }

        public string? SizeEstimateField { get; set; } = string.Empty;

        public List<TeamDto> InvolvedTeams { get; set; } = new List<TeamDto>();
    }
}
