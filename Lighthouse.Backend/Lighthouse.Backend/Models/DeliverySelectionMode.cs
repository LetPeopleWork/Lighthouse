namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// How a Delivery decides which Features belong to it. The database stores the bare number, not
    /// the name, so a member may only ever be added at the end: giving an existing member a different
    /// number re-reads every saved Delivery as something it never was, and nothing about that goes
    /// wrong loudly.
    /// </summary>
    public enum DeliverySelectionMode
    {
        Manual = 0,

        RuleBased = 1,

        SourceBound = 2
    }
}
