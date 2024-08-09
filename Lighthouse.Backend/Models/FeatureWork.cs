namespace Lighthouse.Backend.Models
{
    public class FeatureWork
    {
        public FeatureWork()
        {            
        }

        public FeatureWork(Team team, int remainingWork, int totalWorkItems, Feature feature)
        {
            Team = team;
            TeamId = team.Id;
            RemainingWorkItems = remainingWork;
            TotalWorkItems = totalWorkItems;
            Feature = feature;
            FeatureId = feature.Id;
        }

        public int Id {  get; set; }

        public Team Team { get; set; }

        public int TeamId { get; set; }

        public int RemainingWorkItems {  get; set; }

        public int TotalWorkItems { get; set; }

        public int FeatureId { get; set; }

        public Feature Feature { get; set; }
    }
}
