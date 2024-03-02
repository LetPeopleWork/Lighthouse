using CMFTAspNet.Services.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMFTAspNet.Models
{
    public class Project : WorkTrackingSystemOptionsOwner<Project>, IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
        
        public SearchBy SearchBy { get; set; }
        
        public List<string> WorkItemTypes { get; set; } = new List<string> { "Epic" };

        [NotMapped]
        public IEnumerable<Team> InvolvedTeams => Features.SelectMany(f => f.RemainingWork).Select(rw => rw.Team).Distinct();

        public string SearchTerm { get; set; }

        public List<Feature> Features { get; } = new List<Feature>();

        public List<Milestone> Milestones { get; } = new List<Milestone>();

        public bool IncludeUnparentedItems { get; set; }

        public int DefaultAmountOfWorkItemsPerFeature { get; set; } = 25;

        public DateTime ProjectUpdateTime { get; set; }

        public void UpdateFeatures(IEnumerable<Feature> features)
        {
            Features.Clear();
            Features.AddRange(features);

            ProjectUpdateTime = DateTime.Now;
        }
    }

    public enum SearchBy
    {
        Tag,
        AreaPath,
    }
}
