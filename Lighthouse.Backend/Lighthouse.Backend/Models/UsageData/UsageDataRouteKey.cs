namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Which page was opened, said as a choice from this list rather than as an address. The Team and
    /// Portfolio detail pages are the two whose address holds both a customer's identifier and the
    /// name of the view someone opened; the identifier is theirs and must not travel, while the view
    /// is one of the few things worth knowing. Naming the pair as a single choice keeps the second
    /// without ever putting the first into a value that can leave the browser.
    /// </summary>
    public enum UsageDataRouteKey
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        TeamDetail_Features = 0,

        TeamDetail_Forecasts = 1,

        TeamDetail_Metrics = 2,

        TeamDetail_Settings = 3,

        TeamDetail_Access = 4,

        PortfolioDetail_Features = 5,

        PortfolioDetail_Metrics = 6,

        PortfolioDetail_Deliveries = 7,

        PortfolioDetail_Settings = 8,

        PortfolioDetail_Access = 9,
    }
}
