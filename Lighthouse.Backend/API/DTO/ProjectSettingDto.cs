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
            DefaultAmountOfWorkItemsPerFeature = project.DefaultAmountOfWorkItemsPerFeature;
            WorkTrackingSystemConnectionId = project.WorkTrackingSystemConnectionId;
            SizeEstimateField = project.SizeEstimateField;
        }

        [JsonRequired]
        public int Id { get; set; } 

        public string Name { get; set; }

        public List<string> WorkItemTypes { get; set; } = [];

        public List<MilestoneDto> Milestones { get; set; } = [];

        public string WorkItemQuery { get; set; }

        public string UnparentedItemsQuery { get; set; }

        [JsonRequired]
        public int DefaultAmountOfWorkItemsPerFeature { get; set; }

        [JsonRequired]
        public int WorkTrackingSystemConnectionId { get; set; }

        public string? SizeEstimateField { get; set; } = string.Empty;
    }
}
