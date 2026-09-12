namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// The address published for each page someone can open. Every one of these is written here
    /// rather than taken from a request, so what a third party is shown can be read off this file -
    /// and since none of them names a particular Team or Portfolio, none of them can.
    /// </summary>
    public static class UsageDataRoutePatterns
    {
        private static readonly Dictionary<UsageDataRouteKey, string> Addresses = new()
        {
            [UsageDataRouteKey.TeamDetail_Features] = "/teams/:id/features",
            [UsageDataRouteKey.TeamDetail_Forecasts] = "/teams/:id/forecasts",
            [UsageDataRouteKey.TeamDetail_Metrics] = "/teams/:id/metrics",
            [UsageDataRouteKey.TeamDetail_Settings] = "/teams/:id/settings",
            [UsageDataRouteKey.TeamDetail_Access] = "/teams/:id/access",
            [UsageDataRouteKey.PortfolioDetail_Features] = "/portfolios/:id/features",
            [UsageDataRouteKey.PortfolioDetail_Metrics] = "/portfolios/:id/metrics",
            [UsageDataRouteKey.PortfolioDetail_Deliveries] = "/portfolios/:id/deliveries",
            [UsageDataRouteKey.PortfolioDetail_Settings] = "/portfolios/:id/settings",
            [UsageDataRouteKey.PortfolioDetail_Access] = "/portfolios/:id/access",
        };

        public static IReadOnlyDictionary<UsageDataRouteKey, string> All => Addresses;
    }
}
