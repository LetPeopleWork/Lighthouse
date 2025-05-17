using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class TeamDto : WorkTrackingSystemOptionsOwnerDtoBase
    {
        public TeamDto(Team team) : base(team)
        {
            FeatureWip = team.FeatureWIP;
            UseFixedDatesForThroughput = team.UseFixedDatesForThroughput;

            var throughputSettings = team.GetThroughputSettings();
            ThroughputStartDate = throughputSettings.StartDate;
            ThroughputEndDate = throughputSettings.EndDate;
        }

        [JsonRequired]
        public int FeatureWip { get; set; }

        public List<FeatureDto> Features { get; } = new List<FeatureDto>();

        public List<ProjectDto> Projects { get; } = new List<ProjectDto>();

        public bool UseFixedDatesForThroughput { get; }

        public DateTime ThroughputStartDate { get; }

        public DateTime ThroughputEndDate { get; }
    }
}
