namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Which ends of a link type an item's parent is read from. Typing the phrase one end reads as says
    /// which way the parent lies; typing the type's own name says nothing about it, so both ends are read.
    /// </summary>
    public enum LinkDirection
    {
        Both,
        Inward,
        Outward,
    }
}
