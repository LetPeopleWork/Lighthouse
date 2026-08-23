namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// One Team's share of one Feature in a forecast, and how often that share finished on each simulated
    /// day. It is what a run produces, not what a run works with: the counts that go up and down while a run
    /// is under way belong to the run that owns them, because ten thousand runs happen at once and two of
    /// them sharing one counter would be wrong even if neither write ever tore.
    /// </summary>
    public class SimulationResult
    {
        public SimulationResult()
        {
        }

        public SimulationResult(Team team, Feature feature, int remainingItems)
        {
            Team = team;
            Feature = feature;
            InitialRemainingItems = remainingItems;
        }

        public Team Team { get; }

        public Feature Feature { get; }

        public int InitialRemainingItems { get; }

        public Dictionary<int, int> SimulationResults { get; } = new Dictionary<int, int>();
    }
}
