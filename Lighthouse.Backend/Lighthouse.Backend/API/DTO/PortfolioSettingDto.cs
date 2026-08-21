using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class PortfolioSettingDto : SettingsOwnerDtoBase
    {
        public PortfolioSettingDto() : base()
        {            
        }

        public PortfolioSettingDto(Portfolio portfolio, ISet<int>? readableTeamIds = null) : base(portfolio)
        {
            UsePercentileToCalculateDefaultAmountOfWorkItems = portfolio.UsePercentileToCalculateDefaultAmountOfWorkItems;
            DefaultAmountOfWorkItemsPerFeature = portfolio.DefaultAmountOfWorkItemsPerFeature;
            DefaultWorkItemPercentile = portfolio.DefaultWorkItemPercentile;
            PercentileHistoryInDays = portfolio.PercentileHistoryInDays;
            OverrideRealChildCountStates = portfolio.OverrideRealChildCountStates;
            DoneItemsCutoffDays = portfolio.DoneItemsCutoffDays;
            ConcurrencyToken = portfolio.ConcurrencyToken;

            InvolvedTeams.AddRange(portfolio.CreateInvolvedTeamDtos(readableTeamIds));

            if (portfolio.OwningTeam != null
                && (readableTeamIds is null || readableTeamIds.Contains(portfolio.OwningTeam.Id)))
            {
                var owningTeam = portfolio.OwningTeam;
                OwningTeam = new EntityReferenceDto(owningTeam.Id, owningTeam.Name);
            }

            SizeEstimateAdditionalFieldDefinitionId = portfolio.SizeEstimateAdditionalFieldDefinitionId;
            FeatureOwnerAdditionalFieldDefinitionId = portfolio.FeatureOwnerAdditionalFieldDefinitionId;
            DependencyOverrideAdditionalFieldDefinitionId = portfolio.DependencyOverrideAdditionalFieldDefinitionId;
            IgnoreDependencies = portfolio.IgnoreDependencies;

            if (portfolio.WorkTrackingSystemConnection != null)
            {
                DataRetrievalSchema = DataRetrievalSchemaDto.ForPortfolio(portfolio.WorkTrackingSystemConnection.WorkTrackingSystem);
            }
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

        public int? SizeEstimateAdditionalFieldDefinitionId { get; set; }

        public int DoneItemsCutoffDays { get; set; } = 365;

        public List<EntityReferenceDto> InvolvedTeams { get; set; } = new List<EntityReferenceDto>();

        public EntityReferenceDto? OwningTeam { get; set; }

        public int? FeatureOwnerAdditionalFieldDefinitionId { get; set; }

        public int? DependencyOverrideAdditionalFieldDefinitionId { get; set; }

        // Nullable rather than required, because every payload written before this setting existed omits
        // it - our own end-to-end helpers, the command line client, and whatever anyone has scripted.
        // Absent has to mean "acts on its dependencies", which is what a Portfolio did before the setting
        // was there; demanding the field instead turns every saved payload into a 400.
        public bool? IgnoreDependencies { get; set; }
    }
}
