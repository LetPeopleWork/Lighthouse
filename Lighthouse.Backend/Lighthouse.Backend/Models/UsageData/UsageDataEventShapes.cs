using System.Collections.Frozen;

namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// What each event is allowed to carry. Two of them are about a page somebody opened and say
    /// which one; the rest are about something somebody did, which happens on no particular page,
    /// and they carry nothing but their name.
    ///
    /// This is a declaration rather than a rule applied at each call site, because the promise it
    /// keeps is that no address travels with an event that has no page. A rule has to be remembered
    /// every time the list grows; a declaration is the thing that grows.
    ///
    /// It also settles a disagreement the vocabulary could not have had while there was one name for
    /// both kinds of page. Now that the name says which kind it is and the address says so too, they
    /// can contradict each other - and a message read straight would be counted as an opening that
    /// never happened, which is a number quietly wrong rather than a message obviously broken.
    /// </summary>
    public static class UsageDataEventShapes
    {
        /// <summary>
        /// The events that are about a page, and the beginning of the address each one's page has.
        /// Read from what this product publishes about its own pages rather than listed again here,
        /// so there is no second copy to fall behind the first.
        /// </summary>
        private static readonly FrozenDictionary<UsageDataEventName, string> ThePageEachEventIsAbout =
            new Dictionary<UsageDataEventName, string>
            {
                [UsageDataEventName.TeamTabOpened] = "/teams/",
                [UsageDataEventName.PortfolioTabOpened] = "/portfolios/",
            }.ToFrozenDictionary();

        public static bool Fits(UsageDataEventName name, UsageDataRouteKey? route)
        {
            if (!ThePageEachEventIsAbout.TryGetValue(name, out var whereItsPagesAddressBegins))
            {
                return route is null;
            }

            return route is { } named
                && UsageDataRoutePatterns.All.TryGetValue(named, out var address)
                && address.StartsWith(whereItsPagesAddressBegins, StringComparison.Ordinal);
        }
    }
}
