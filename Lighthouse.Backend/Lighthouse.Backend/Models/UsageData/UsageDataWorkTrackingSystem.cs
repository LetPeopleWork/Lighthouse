namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Which kind of work tracking system was connected. Deliberately its own list rather than the
    /// one the rest of the product stores connections under: that one is append-only because it is
    /// persisted by position, and binding what leaves an instance to a list kept in that shape lets
    /// a storage concern decide what a third party is shown.
    ///
    /// The kind is the whole of what travels. Never the address of a system, never its name, and
    /// never anything somebody typed while setting it up - there is no field here that could carry
    /// any of those.
    /// </summary>
    public enum UsageDataWorkTrackingSystem
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        AzureDevOps = 0,

        Jira = 1,

        Linear = 2,

        Csv = 3,

        ServiceNow = 4,
    }
}
