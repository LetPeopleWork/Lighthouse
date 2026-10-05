namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>Where the ready count sits against the range the Team is likely to pull before its next Refinement; both ends belong to the range.</summary>
    public enum RefinementVerdict
    {
        Below = 0,
        In = 1,
        Above = 2,
    }
}
