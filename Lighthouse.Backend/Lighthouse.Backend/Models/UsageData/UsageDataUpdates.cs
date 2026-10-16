namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Whether the Teams and Portfolios on a connection were updated straight after its Additional
    /// Fields were changed, or left to their next scheduled update. That is the whole of what the
    /// event says - never the connection, the field, or how many Teams and Portfolios there are.
    /// </summary>
    public enum UsageDataUpdates
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        Now = 0,

        Later = 1,
    }
}
