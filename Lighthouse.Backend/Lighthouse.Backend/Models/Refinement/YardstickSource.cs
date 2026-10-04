namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>Where the number of days a vote is cast against comes from. The numbers are stored, so they never change.</summary>
    public enum YardstickSource
    {
        Sle = 0,
        CycleTimeFallback = 1,
        Unavailable = 2,
    }
}
