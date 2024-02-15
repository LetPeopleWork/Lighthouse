namespace CMFTAspNet.Models
{
    public class SimulationResult
    {
        public SimulationResult(Team team, Feature feature, int remainingItems)
        {
            Team = team;
            Feature = feature;
            InitialRemainingItems = remainingItems;
            RemainingItems = remainingItems;
        }

        public Team Team { get; }

        public Feature Feature { get; }

        public int InitialRemainingItems { get; }

        public int RemainingItems { get; set; }

        public bool HasWorkRemaining { get => RemainingItems > 0; }

        public Dictionary<int, int> SimulationResults { get; } = new Dictionary<int, int>();

        public void ResetRemainingItems()
        {
            RemainingItems = InitialRemainingItems;
        }
    }

    public static class SimulationResultExtensions
    {
        public static int GetRemainingItems(this IEnumerable<SimulationResult> results)
        {
            return results.Sum(x => x.RemainingItems);
        }
        public static void ResetRemainingItems(this IEnumerable<SimulationResult> results)
        {
            foreach (var result in results)
            {
                result.ResetRemainingItems();
            }
        }
    }
}
