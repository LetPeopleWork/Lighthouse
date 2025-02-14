using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class TeamDto
    {
        public TeamDto()
        {
        }

        public TeamDto(Team team)
        {
            Name = team.Name;
            Id = team.Id;
            FeatureWip = team.FeatureWIP;
            LastUpdated = team.TeamUpdateTime;
            FeaturesInProgress = team.FeaturesInProgress;
            Throughput = team.RawThroughput;
            UseFixedDatesForThroughput = team.UseFixedDatesForThroughput;

            var throughputSettings = team.GetThroughputSettings();
            ThroughputStartDate = throughputSettings.StartDate;
            ThroughputEndDate = throughputSettings.EndDate;
        }

        public string Name { get; set; }

        [JsonRequired]
        public int Id { get; set; }

        [JsonRequired]
        public int FeatureWip { get; set; }

        public List<string> FeaturesInProgress { get; } = new List<string>();

        public int[] Throughput { get; set; }

        [JsonRequired]
        public DateTime LastUpdated { get; set; }

        public List<FeatureDto> Features { get; } = new List<FeatureDto>();

        public List<ProjectDto> Projects { get; } = new List<ProjectDto>();

        public bool UseFixedDatesForThroughput { get; }

        public DateTime ThroughputStartDate { get; }

        public DateTime ThroughputEndDate { get; }
    }
}
