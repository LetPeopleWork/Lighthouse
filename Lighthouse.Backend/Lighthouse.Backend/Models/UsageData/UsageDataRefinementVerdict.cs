namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Which verdict the Refinement tab showed when it was opened on a Refinement day: fewer Work
    /// Items ready than the Team needs, enough, more than enough, or no number at all. That is the
    /// whole of what the event says - never the range, how many were ready, the date, the Team or
    /// any Work Item.
    ///
    /// A closed list, so the event can only ever carry one of the values named here and never a
    /// free value of its own.
    /// </summary>
    public enum UsageDataRefinementVerdict
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        Below = 0,
        In = 1,
        Above = 2,
        None = 3,
    }
}
