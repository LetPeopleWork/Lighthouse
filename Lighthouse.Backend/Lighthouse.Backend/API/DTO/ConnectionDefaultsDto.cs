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
            ParentOverrideForTeams = connection.ParentOverrideForTeams;
            ParentOverrideForPortfolios = connection.ParentOverrideForPortfolios;
            DependencyFieldForPortfolios = connection.DependencyFieldForPortfolios;
        }

        // Which members the save actually sent. A member left out keeps what the connection holds, so a tab
        // opened before a default existed, or an older client, cannot clear it by not knowing it. Only an
        // explicit null clears a default.
        private readonly HashSet<ConnectionDefaultSetting> sent = [];

        public int? ParentOverrideForTeams
        {
            get;
            set => field = Sent(ConnectionDefaultSetting.ParentOverrideForTeams, value);
        }

        public int? ParentOverrideForPortfolios
        {
            get;
            set => field = Sent(ConnectionDefaultSetting.ParentOverrideForPortfolios, value);
        }

        public int? DependencyFieldForPortfolios
        {
            get;
            set => field = Sent(ConnectionDefaultSetting.DependencyFieldForPortfolios, value);
        }

        public bool Sends(ConnectionDefaultSetting setting) => sent.Contains(setting);

        private int? Sent(ConnectionDefaultSetting setting, int? value)
        {
            sent.Add(setting);
            return value;
        }

        // The defaults this build cannot hold yet. They are caught here rather than dropped, so a save that
        // names one is refused instead of appearing to succeed.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? NotHeldByThisBuild { get; set; }

        public IEnumerable<string> NamedButNotHeld()
            => NotHeldByThisBuild?.Where(named => named.Value.ValueKind != JsonValueKind.Null).Select(named => named.Key) ?? [];

        public IEnumerable<ConnectionDefaultSetting> Named()
        {
            if (ParentOverrideForTeams.HasValue)
            {
                yield return ConnectionDefaultSetting.ParentOverrideForTeams;
            }

            if (ParentOverrideForPortfolios.HasValue)
            {
                yield return ConnectionDefaultSetting.ParentOverrideForPortfolios;
            }

            if (DependencyFieldForPortfolios.HasValue)
            {
                yield return ConnectionDefaultSetting.DependencyFieldForPortfolios;
            }
        }
    }
}
