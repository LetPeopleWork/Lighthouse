namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>What one entry of the sizing log records. The numbers are stored, so they never change.</summary>
    public enum SizingEntryKind
    {
        Vote = 0,
        Comment = 1,
        Revocation = 2,
    }
}
