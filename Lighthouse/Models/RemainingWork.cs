namespace Lighthouse.Models
{
    public class RemainingWork
    {
        public RemainingWork()
        {            
        }

        public RemainingWork(Team team, int remainingWork, Feature feature)
        {
            Team = team;
            TeamId = team.Id;
            RemainingWorkItems = remainingWork;
            Feature = feature;
            FeatureId = feature.Id;
        }

        public int Id {  get; set; }

        public Team Team { get; set; }

        public int TeamId { get; set; }

        public int RemainingWorkItems {  get; set; }

        public int FeatureId { get; set; }

        public Feature Feature { get; set; }
    }
}
