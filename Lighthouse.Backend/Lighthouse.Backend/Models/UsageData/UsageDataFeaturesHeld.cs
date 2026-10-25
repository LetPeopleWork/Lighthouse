namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Roughly how many Features the page somebody opened holds, as one of six bands. That is the
    /// whole of what the event says about its page - never the count itself, which on its own
    /// could single out one instance.
    /// </summary>
    public enum UsageDataFeaturesHeld
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        UpTo20 = 0,

        From21To50 = 1,

        From51To100 = 2,

        From101To399 = 3,

        From400 = 4,

        Unknown = 5,
    }
}
