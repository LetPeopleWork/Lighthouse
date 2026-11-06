using Lighthouse.Backend.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>What a save means for a default that holds a list of parent sources; Outcome is null when it clears the default.</summary>
    public sealed record ParentSourcesChoice(ParentSourcesOutcome? Outcome);

    /// <summary>
    /// The settings a connection holds for every Team or Portfolio on it that follows them: an Additional Field id each,
    /// the parent sources as an ordered list, and an Estimation bundle per grain.
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
                sent[fieldDefault.Setting] = fieldDefault.FirstFieldOn(connection);

                if (fieldDefault.ParentSourcesOn is { } parentSourcesOn)
                {
                    lists[fieldDefault.Setting] = parentSourcesOn(connection) is { } list ? ParentSourceEntryDto.ListOf(list) : null;
                }
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

        // The list of parent sources sent for each grain, beside its old single field in the map above.
        private readonly Dictionary<ConnectionDefaultSetting, List<ParentSourceEntryDto?>?> lists = [];

        /// <summary>Deprecated: the first field of <see cref="ParentSourcesForTeams"/>, for clients that only know one field.</summary>
        public int? ParentOverrideForTeams
        {
            get => FieldOf(ConnectionDefaultSetting.ParentOverrideForTeams);
            set => sent[ConnectionDefaultSetting.ParentOverrideForTeams] = value;
        }

        /// <summary>Deprecated: the first field of <see cref="ParentSourcesForPortfolios"/>, for clients that only know one field.</summary>
        public int? ParentOverrideForPortfolios
        {
            get => FieldOf(ConnectionDefaultSetting.ParentOverrideForPortfolios);
            set => sent[ConnectionDefaultSetting.ParentOverrideForPortfolios] = value;
        }

        public List<ParentSourceEntryDto?>? ParentSourcesForTeams
        {
            get => lists.GetValueOrDefault(ConnectionDefaultSetting.ParentOverrideForTeams);
            set => lists[ConnectionDefaultSetting.ParentOverrideForTeams] = value;
        }

        public List<ParentSourceEntryDto?>? ParentSourcesForPortfolios
        {
            get => lists.GetValueOrDefault(ConnectionDefaultSetting.ParentOverrideForPortfolios);
            set => lists[ConnectionDefaultSetting.ParentOverrideForPortfolios] = value;
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

        public bool Sends(ConnectionDefaultSetting setting) => sent.ContainsKey(setting) || lists.ContainsKey(setting);

        /// <summary>The field the save names for a default; null when it clears the default or does not send it.</summary>
        public int? FieldOf(ConnectionDefaultSetting setting) => sent.GetValueOrDefault(setting);

        /// <summary>Every field the save names for a default: the whole list when it means one, else the single field.</summary>
        public IReadOnlyList<int> FieldsOf(ConnectionDefaultSetting setting, ParentSources? held)
        {
            if (ParentSourcesChoiceFor(setting, held) is { } choice)
            {
                return choice.Outcome?.List?.AdditionalFieldIds() ?? [];
            }

            return FieldOf(setting) is { } field ? [field] : [];
        }

        /// <summary>
        /// What the save means by a default that holds a list; null when it sends no list or when the old single
        /// field is the edit.
        /// </summary>
        public ParentSourcesChoice? ParentSourcesChoiceFor(ConnectionDefaultSetting setting, ParentSources? held)
        {
            if (!lists.TryGetValue(setting, out var sentList)
                || (sent.TryGetValue(setting, out var oldField) && ParentSourceEntryDto.TheOldFieldIsTheEdit(oldField, sentList, held)))
            {
                return null;
            }

            return new(sentList is null ? null : ParentSourceEntryDto.OutcomeOf(sentList));
        }

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
            => ConnectionFieldDefaults.All.Select(fieldDefault => fieldDefault.Setting)
                .Where(setting => FieldOf(setting).HasValue || lists.GetValueOrDefault(setting) is not null);
    }
}
