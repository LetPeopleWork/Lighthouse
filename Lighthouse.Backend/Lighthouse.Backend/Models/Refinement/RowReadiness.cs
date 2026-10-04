namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>What a Work Item's votes make of it under the Team's readiness setting. Worked out on every read, never stored.</summary>
    public enum RowReadiness
    {
        Ready = 0,
        MoreYesNeeded = 1,
        MoreVotersNeeded = 2,
        NeedsDiscussion = 3,
    }
}
