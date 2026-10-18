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
            SizeEstimateSource = portfolio.SizeEstimateFieldSource;
            SizeEstimateDefaultName = portfolio.SizeEstimateFieldDefaultName;
            FeatureOwnerAdditionalFieldDefinitionId = portfolio.FeatureOwnerAdditionalFieldDefinitionId;
            DependencyOverrideAdditionalFieldDefinitionId = portfolio.DependencyOverrideAdditionalFieldDefinitionId;
            DependencyFieldSource = portfolio.DependencyFieldSource;
            DependencyFieldDefaultName = portfolio.DependencyFieldDefaultName;
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

        [JsonConverter(typeof(JsonStringEnumConverter<SettingSource>))]
        public SettingSource? SizeEstimateSource { get; set; }

        public string? SizeEstimateDefaultName { get; private set; }

        public int DoneItemsCutoffDays { get; set; } = 365;

        public List<EntityReferenceDto> InvolvedTeams { get; set; } = new List<EntityReferenceDto>();

        public EntityReferenceDto? OwningTeam { get; set; }

        public int? FeatureOwnerAdditionalFieldDefinitionId { get; set; }

        public int? DependencyOverrideAdditionalFieldDefinitionId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter<SettingSource>))]
        public SettingSource? DependencyFieldSource { get; set; }

        public string? DependencyFieldDefaultName { get; private set; }

        // Nullable rather than required, because every payload written before this setting existed omits
        // it - our own end-to-end helpers, the command line client, and whatever anyone has scripted.
        // Absent keeps what the Portfolio holds, so a script that does not know the switch cannot turn it
        // off; a new Portfolio holds "acts on its dependencies", which is what one did before the setting
        // was there. Demanding the field instead turns every saved payload into a 400.
        public bool? IgnoreDependencies { get; set; }
    }
}
