namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Which type of report was created or opened. That is the whole of what those events say -
    /// never the report's name, its Team or Portfolio, its dates or its numbers.
    ///
    /// Usage data's own list rather than the product's names for report types, so a new type of
    /// report says nothing until somebody appends it here and the usage data page says so too.
    /// </summary>
    public enum UsageDataReportTemplate
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        ThenAndNow = 0,
    }
}
