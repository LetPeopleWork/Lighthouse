namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// When a sizing vote was cast, relative to the Team's Refinement. That is the whole of what a
    /// sizing event says: it answers whether votes arrive outside the meeting, and nothing about the
    /// Team, the Work Item, the answer or whoever voted travels with it.
    ///
    /// A closed list, so the event can only ever carry one of the values named here and never a
    /// free value of its own. It does not hide when the vote happened: like every other event, it
    /// travels with the moment the browser recorded it. A Team with no Refinement cadence has only
    /// one answer.
    /// </summary>
    public enum UsageDataSizingMoment
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        NoCadence = 0,
        OnRefinementDay = 1,
        OnOtherDay = 2,
    }
}
