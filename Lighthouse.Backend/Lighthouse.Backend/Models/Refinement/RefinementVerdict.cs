namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>Where the ready count sits against the range the Team is likely to pull over one Refinement cycle; both ends belong to the range.</summary>
    public enum RefinementVerdict
    {
        Below = 0,
        In = 1,
        Above = 2,
    }
}
