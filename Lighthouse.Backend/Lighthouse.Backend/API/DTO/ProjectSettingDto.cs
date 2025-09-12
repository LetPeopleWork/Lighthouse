using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class ProjectSettingDto : SettingsOwnerDtoBase
    {
        public ProjectSettingDto() : base()
        {            
        }

        public ProjectSettingDto(Project project) : base(project)
        {
            Milestones.AddRange(project.Milestones.Select(m => new MilestoneDto(m)));
            UnparentedItemsQuery = project.UnparentedItemsQuery;

            UsePercentileToCalculateDefaultAmountOfWorkItems = project.UsePercentileToCalculateDefaultAmountOfWorkItems;
            DefaultAmountOfWorkItemsPerFeature = project.DefaultAmountOfWorkItemsPerFeature;
            DefaultWorkItemPercentile = project.DefaultWorkItemPercentile;
            PercentileHistoryInDays = project.PercentileHistoryInDays;
            SizeEstimateField = project.SizeEstimateField;
            OverrideRealChildCountStates = project.OverrideRealChildCountStates;

            InvolvedTeams.AddRange(project.CreateInvolvedTeamDtos());

            if (project.OwningTeam != null)
            {
                var owningTeam = project.OwningTeam;
                OwningTeam = new EntityReferenceDto(owningTeam.Id, owningTeam.Name);
            }

            FeatureOwnerField = project.FeatureOwnerField;
        }

        public List<MilestoneDto> Milestones { get; set; } = [];

        public List<string> OverrideRealChildCountStates { get; set; } = [];

        public string? UnparentedItemsQuery { get; set; }

        [JsonRequired]
        public bool UsePercentileToCalculateDefaultAmountOfWorkItems { get; set; }

        [JsonRequired]
        public int DefaultWorkItemPercentile { get; set; }

        [JsonRequired]
        public int? PercentileHistoryInDays { get; set; }

        [JsonRequired]
        public int DefaultAmountOfWorkItemsPerFeature { get; set; }

        public string? SizeEstimateField { get; set; } = string.Empty;

        public List<EntityReferenceDto> InvolvedTeams { get; set; } = new List<EntityReferenceDto>();

        public EntityReferenceDto? OwningTeam { get; set; }

        public string? FeatureOwnerField { get; set; }
    }
}
