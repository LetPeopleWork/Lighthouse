namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Everything a browser is allowed to report. A name that is not on this list does not arrive as
    /// an unknown event to be dealt with later - it fails to be read at all, so an event nobody wrote
    /// down cannot be sent by accident or on purpose.
    /// </summary>
    public enum UsageDataEventName
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        TeamOrPortfolioTabOpened = 0,
    }
}
