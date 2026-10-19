using Lighthouse.Backend.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// The settings a connection holds for every Team or Portfolio on it that follows them: an Additional Field id each,
    /// and an Estimation bundle per grain.
    /// </summary>
    public class ConnectionDefaultsDto
    {
        public ConnectionDefaultsDto()
        {
        }

        public ConnectionDefaultsDto(WorkTrackingSystemConnection connection)
        {
            foreach (var fieldDefault in ConnectionFieldDefaults.All)
            {
                sent[fieldDefault.Setting] = fieldDefault.FieldOn(connection);
            }

            estimations[ConnectionDefaultSetting.EstimationForTeams] = EstimationDefaultDto.From(connection.EstimationForTeams);
            estimations[ConnectionDefaultSetting.EstimationForPortfolios] = EstimationDefaultDto.From(connection.EstimationForPortfolios);
        }

        // The field the save actually sent for each default. A member left out keeps what the connection holds,
        // so a tab opened before a default existed, or an older client, cannot clear it by not knowing it. Only
        // an explicit null clears a default.
        private readonly Dictionary<ConnectionDefaultSetting, int?> sent = [];

        // The whole Estimation bundle sent for each grain, beside its field in the map above.
        private readonly Dictionary<ConnectionDefaultSetting, EstimationDefaultDto?> estimations = [];

        public int? ParentOverrideForTeams
        {
            get => FieldOf(ConnectionDefaultSetting.ParentOverrideForTeams);
            set => sent[ConnectionDefaultSetting.ParentOverrideForTeams] = value;
        }

        public int? ParentOverrideForPortfolios
        {
            get => FieldOf(ConnectionDefaultSetting.ParentOverrideForPortfolios);
            set => sent[ConnectionDefaultSetting.ParentOverrideForPortfolios] = value;
        }

        public int? DependencyFieldForPortfolios
        {
            get => FieldOf(ConnectionDefaultSetting.DependencyFieldForPortfolios);
            set => sent[ConnectionDefaultSetting.DependencyFieldForPortfolios] = value;
        }

        public int? SizeEstimateFieldForPortfolios
        {
            get => FieldOf(ConnectionDefaultSetting.SizeEstimateFieldForPortfolios);
            set => sent[ConnectionDefaultSetting.SizeEstimateFieldForPortfolios] = value;
        }

        public int? FeatureOwnerFieldForPortfolios
        {
            get => FieldOf(ConnectionDefaultSetting.FeatureOwnerFieldForPortfolios);
            set => sent[ConnectionDefaultSetting.FeatureOwnerFieldForPortfolios] = value;
        }

        public EstimationDefaultDto? EstimationForTeams
        {
            get => estimations.GetValueOrDefault(ConnectionDefaultSetting.EstimationForTeams);
            set => SendEstimation(ConnectionDefaultSetting.EstimationForTeams, value);
        }

        public EstimationDefaultDto? EstimationForPortfolios
        {
            get => estimations.GetValueOrDefault(ConnectionDefaultSetting.EstimationForPortfolios);
            set => SendEstimation(ConnectionDefaultSetting.EstimationForPortfolios, value);
        }

        public bool Sends(ConnectionDefaultSetting setting) => sent.ContainsKey(setting);

        /// <summary>The field the save names for a default; null when it clears the default or does not send it.</summary>
        public int? FieldOf(ConnectionDefaultSetting setting) => sent.GetValueOrDefault(setting);

        /// <summary>The Estimation bundle the save sends for a default; None when it clears it, null when it sends none.</summary>
        public EstimationBundle? BundleOf(ConnectionDefaultSetting setting)
            => estimations.TryGetValue(setting, out var estimation) ? estimation?.ToBundle() ?? EstimationBundle.None : null;

        private void SendEstimation(ConnectionDefaultSetting setting, EstimationDefaultDto? estimation)
        {
            estimations[setting] = estimation;
            sent[setting] = estimation?.ToBundle().AdditionalFieldDefinitionId;
        }

        // The defaults this build cannot hold yet. They are caught here rather than dropped, so a save that
        // names one is refused instead of appearing to succeed.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? NotHeldByThisBuild { get; set; }

        public IEnumerable<string> NamedButNotHeld()
            => NotHeldByThisBuild?.Where(named => named.Value.ValueKind != JsonValueKind.Null).Select(named => named.Key) ?? [];

        public IEnumerable<ConnectionDefaultSetting> Named()
            => ConnectionFieldDefaults.All.Select(fieldDefault => fieldDefault.Setting).Where(setting => FieldOf(setting).HasValue);
    }
}
