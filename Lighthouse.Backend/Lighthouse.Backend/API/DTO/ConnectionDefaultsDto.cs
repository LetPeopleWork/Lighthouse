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
        }

        public int? ParentOverrideForTeams { get; set; }

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
        }
    }
}
