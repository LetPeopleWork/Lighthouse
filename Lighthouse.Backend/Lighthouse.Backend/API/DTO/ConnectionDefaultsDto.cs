using Lighthouse.Backend.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>The settings a connection holds for every Team or Portfolio on it that follows them, each an Additional Field id.</summary>
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
        }

        // The field the save actually sent for each default. A member left out keeps what the connection holds,
        // so a tab opened before a default existed, or an older client, cannot clear it by not knowing it. Only
        // an explicit null clears a default.
        private readonly Dictionary<ConnectionDefaultSetting, int?> sent = [];

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

        public bool Sends(ConnectionDefaultSetting setting) => sent.ContainsKey(setting);

        /// <summary>The field the save names for a default; null when it clears the default or does not send it.</summary>
        public int? FieldOf(ConnectionDefaultSetting setting) => sent.GetValueOrDefault(setting);

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
