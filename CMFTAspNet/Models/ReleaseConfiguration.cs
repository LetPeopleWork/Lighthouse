using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Models
{
    public class ReleaseConfiguration
    {

        public string Name { get; set; }
        
        public SearchBy SearchBy { get; set; }
        
        public List<string> WorkItemTypes { get; set; } = new List<string>();

        public List<Team> InvolvedTeams { get; set; } = new List<Team>();

        public string SearchTerm { get; set; }

        public DateTime TargetDate { get;set; }

        public List<Feature> Features { get; } = new List<Feature>();

        public bool IncludeUnparentedItems { get; set; }
    }

    public enum SearchBy
    {
        Tag,
        AreaPath,
    }
}
