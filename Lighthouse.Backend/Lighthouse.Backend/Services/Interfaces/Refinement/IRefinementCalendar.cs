using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Interfaces.Refinement
{
    /// <summary>What a Team's Refinement cadence says about the instance's today.</summary>
    public interface IRefinementCalendar
    {
        RefinementCalendarFacts FactsFor(RefinementCadence? cadence);
    }

    /// <param name="NextRefinementDate">The first cadence day strictly after today that is not blacked out; null without a cadence.</param>
    /// <param name="IsRefinementDay">Whether today itself is a cadence day that is not blacked out.</param>
    /// <param name="DaysUntilNextRefinement">
    /// Calendar days from the instance's today to <paramref name="NextRefinementDate"/>, so a browser in another time
    /// zone counts from the same day the date was found from; null when there is no next Refinement.
    /// </param>
    public sealed record RefinementCalendarFacts(DateOnly? NextRefinementDate, bool IsRefinementDay, int? DaysUntilNextRefinement)
    {
        public static RefinementCalendarFacts None { get; } = new(null, false, null);
    }
}
