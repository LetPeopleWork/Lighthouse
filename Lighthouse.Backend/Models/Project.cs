using System.ComponentModel.DataAnnotations.Schema;

namespace Lighthouse.Backend.Models
{
    public class Project : WorkTrackingSystemOptionsOwner
    {
        public string Name { get; set; }

        public List<string> WorkItemTypes { get; set; } = new List<string> { "Epic" };

        public List<Team> Teams { get; } = new List<Team>();

        public List<Feature> Features { get; } = [];

        public List<Milestone> Milestones { get; } = new List<Milestone>();

        public int DefaultAmountOfWorkItemsPerFeature { get; set; } = 25;

        public DateTime ProjectUpdateTime { get; set; }

        public string? UnparentedItemsQuery { get; set; }

        public string? SizeEstimateField { get; set; }

        public bool UsePercentileToCalculateDefaultAmountOfWorkItems { get; set; } = false;

        public string HistoricalFeaturesWorkItemQuery { get; set; } = string.Empty;

        public int DefaultWorkItemPercentile { get; set; } = 85;

        public List<string> OverrideRealChildCountStates { get; set; } = new List<string>();

        public void UpdateFeatures(IEnumerable<Feature> features)
        {
            Features.Clear();
            Features.AddRange(features);

            ProjectUpdateTime = DateTime.UtcNow;
        }

        public void UpdateTeams(IEnumerable<Team> teams)
        {
            Teams.Clear();
            Teams.AddRange(teams);

            ProjectUpdateTime = DateTime.UtcNow;
        }
    }
}
