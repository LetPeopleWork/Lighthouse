using System.ComponentModel.DataAnnotations.Schema;

namespace Lighthouse.Backend.Models
{
    public class Project : WorkTrackingSystemOptionsOwner
    {
        public string Name { get; set; }
        
        public List<string> WorkItemTypes { get; set; } = new List<string> { "Epic" };

        [NotMapped]
        public IEnumerable<Team> InvolvedTeams => Features.SelectMany(f => f.RemainingWork).Select(rw => rw.Team).Distinct();

        public List<Feature> Features { get; } = [];

        public List<Milestone> Milestones { get; } = new List<Milestone>();

        public int DefaultAmountOfWorkItemsPerFeature { get; set; } = 25;

        public DateTime ProjectUpdateTime { get; set; }

        public string? UnparentedItemsQuery { get; set; }

        public void UpdateFeatures(IEnumerable<Feature> features)
        {
            Features.Clear();
            Features.AddRange(features);

            ProjectUpdateTime = DateTime.UtcNow;
        }
    }
}
