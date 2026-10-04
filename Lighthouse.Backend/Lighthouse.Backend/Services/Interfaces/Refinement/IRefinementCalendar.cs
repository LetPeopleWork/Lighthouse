using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Interfaces.Refinement
{
    /// <summary>What a Team's Refinement cadence says about the instance's today.</summary>
    public interface IRefinementCalendar
    {
        RefinementCalendarFacts FactsFor(RefinementCadence? cadence);
    }

    /// <param name="NextRefinementDate">The first cadence day strictly after today; null without a cadence.</param>
    /// <param name="IsRefinementDay">Whether today itself is a cadence day.</param>
    public sealed record RefinementCalendarFacts(DateOnly? NextRefinementDate, bool IsRefinementDay)
    {
        public static RefinementCalendarFacts None { get; } = new(null, false);
    }
}
