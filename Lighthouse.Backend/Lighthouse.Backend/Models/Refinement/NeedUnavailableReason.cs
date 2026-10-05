namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>Why the tab gives no range to refine towards.</summary>
    public enum NeedUnavailableReason
    {
        NoCadence = 0,
        InsufficientData = 1,
        NoRefinementStates = 2,
    }
}
