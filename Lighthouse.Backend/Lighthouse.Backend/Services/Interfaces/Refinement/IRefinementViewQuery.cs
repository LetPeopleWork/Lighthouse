using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Refinement
{
    public interface IRefinementViewQuery
    {
        /// <returns>Null when there is no such Team.</returns>
        RefinementView? ForTeam(int teamId);
    }

    public sealed record RefinementView(bool RefinementConfigured, List<WorkItem> WorkItems, Yardstick Yardstick);

    /// <summary>The number every voter answers against: so many days, with so much probability.</summary>
    public sealed record Yardstick(YardstickSource Source, int? Days, int? Probability)
    {
        public static Yardstick None { get; } = new(YardstickSource.Unavailable, null, null);
    }

    // The ordinals are stored, so new sources are appended and existing ones never renumbered.
    public enum YardstickSource
    {
        Sle = 0,
        CycleTimeFallback = 1,
        Unavailable = 2,
    }
}
