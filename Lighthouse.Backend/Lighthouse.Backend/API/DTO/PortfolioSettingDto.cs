using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class PortfolioSettingDto : SettingsOwnerDtoBase
    {
        public PortfolioSettingDto() : base()
        {            
        }

        public PortfolioSettingDto(Portfolio portfolio) : base(portfolio)
        {
            UsePercentileToCalculateDefaultAmountOfWorkItems = portfolio.UsePercentileToCalculateDefaultAmountOfWorkItems;
            DefaultAmountOfWorkItemsPerFeature = portfolio.DefaultAmountOfWorkItemsPerFeature;
            DefaultWorkItemPercentile = portfolio.DefaultWorkItemPercentile;
            PercentileHistoryInDays = portfolio.PercentileHistoryInDays;
            SizeEstimateField = portfolio.SizeEstimateField;
            OverrideRealChildCountStates = portfolio.OverrideRealChildCountStates;
            DoneItemsCutoffDays = portfolio.DoneItemsCutoffDays;

            InvolvedTeams.AddRange(portfolio.CreateInvolvedTeamDtos());

            if (portfolio.OwningTeam != null)
            {
                var owningTeam = portfolio.OwningTeam;
                OwningTeam = new EntityReferenceDto(owningTeam.Id, owningTeam.Name);
            }

            FeatureOwnerField = portfolio.FeatureOwnerField;
        }

        public List<string> OverrideRealChildCountStates { get; set; } = [];

        [JsonRequired]
        public bool UsePercentileToCalculateDefaultAmountOfWorkItems { get; set; }

        [JsonRequired]
        public int DefaultWorkItemPercentile { get; set; }

        [JsonRequired]
        public int? PercentileHistoryInDays { get; set; }

        [JsonRequired]
        public int DefaultAmountOfWorkItemsPerFeature { get; set; }

        public string? SizeEstimateField { get; set; } = string.Empty;

        [JsonRequired]
        public int DoneItemsCutoffDays { get; set; }

        public List<EntityReferenceDto> InvolvedTeams { get; set; } = new List<EntityReferenceDto>();

        public EntityReferenceDto? OwningTeam { get; set; }

        public string? FeatureOwnerField { get; set; }
    }
}
