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
            HistoricalFeaturesWorkItemQuery = project.HistoricalFeaturesWorkItemQuery;
            SizeEstimateField = project.SizeEstimateField;
            OverrideRealChildCountStates = project.OverrideRealChildCountStates;

            InvolvedTeams.AddRange(project.CreateInvolvedTeamDtos());

            if (project.OwningTeam != null)
            {
                OwningTeam = new TeamDto(project.OwningTeam);
            }

            FeatureOwnerField = project.FeatureOwnerField;
        }

        public List<MilestoneDto> Milestones { get; set; } = [];

        public List<string> OverrideRealChildCountStates { get; set; } = [];

        public string UnparentedItemsQuery { get; set; }

        [JsonRequired]
        public bool UsePercentileToCalculateDefaultAmountOfWorkItems { get; set; }

        [JsonRequired]
        public int DefaultWorkItemPercentile { get; set; }

        public string HistoricalFeaturesWorkItemQuery { get; set; }

        [JsonRequired]
        public int DefaultAmountOfWorkItemsPerFeature { get; set; }

        public string? SizeEstimateField { get; set; } = string.Empty;

        public List<TeamDto> InvolvedTeams { get; set; } = new List<TeamDto>();

        public TeamDto? OwningTeam { get; set; }

        public string? FeatureOwnerField { get; set; }
    }
}
