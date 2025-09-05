using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class TeamDto : WorkTrackingSystemOptionsOwnerDtoBase
    {
        public TeamDto() : base()
        {            
        }

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

        public List<EntityReferenceDto> Features { get; } = new List<EntityReferenceDto>();

        public List<EntityReferenceDto> Projects { get; } = new List<EntityReferenceDto>();

        public bool UseFixedDatesForThroughput { get; }

        public DateTime ThroughputStartDate { get; }

        public DateTime ThroughputEndDate { get; }
    }
}
