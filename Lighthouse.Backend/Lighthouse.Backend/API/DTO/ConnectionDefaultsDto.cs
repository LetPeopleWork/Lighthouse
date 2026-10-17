using Lighthouse.Backend.Models;

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
    }
}
