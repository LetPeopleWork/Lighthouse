using System.Collections.Frozen;

namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// What each event is allowed to carry. Two of them are about a page somebody opened and say
    /// which one; one says which kind of work tracking system was connected; one says which setting
    /// was switched and which way; the rest are about something somebody did, which happens on no
    /// particular page, and they carry nothing but their name.
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

        /// <summary>
        /// The one event that says which kind of work tracking system it was, because that is the
        /// whole reason it exists - it answers whether a connector anybody built is being used. One
        /// arriving without it would be counted as a connection of no particular kind, which is
        /// worse than not counting it.
        /// </summary>
        private static readonly FrozenSet<UsageDataEventName> EventsThatSayWhichKindOfSystem =
            FrozenSet.ToFrozenSet([UsageDataEventName.WorkTrackingSystemConnected]);

        /// <summary>
        /// The one event that says a setting was switched. It carries which setting and whether it is
        /// now on or off, and only ever the two together: which setting without the direction says
        /// nothing anybody can count, and a direction without the setting could be any switch at all.
        /// </summary>
        private static readonly FrozenSet<UsageDataEventName> EventsThatSayWhichSettingWasSwitched =
            FrozenSet.ToFrozenSet([UsageDataEventName.OptionalFeatureToggled]);

        public static bool Fits(
            UsageDataEventName name,
            UsageDataRouteKey? route,
            UsageDataWorkTrackingSystem? system,
            UsageDataOptionalFeature? setting,
            bool? enabled)
        {
            return NamesItsPage(name, route)
                && NamesAKindOfSystemExactlyWhenItShould(name, system)
                && NamesASwitchedSettingExactlyWhenItShould(name, setting, enabled);
        }

        private static bool NamesItsPage(UsageDataEventName name, UsageDataRouteKey? route)
        {
            if (!ThePageEachEventIsAbout.TryGetValue(name, out var whereItsPagesAddressBegins))
            {
                return route is null;
            }

            return route is { } named
                && UsageDataRoutePatterns.All.TryGetValue(named, out var address)
                && address.StartsWith(whereItsPagesAddressBegins, StringComparison.Ordinal);
        }

        /// <summary>
        /// Both ways round, deliberately. An event that should name a kind and does not is refused,
        /// and so is one that names a kind it has no business naming - without the second half the
        /// declaration would be advice rather than a boundary.
        /// </summary>
        private static bool NamesAKindOfSystemExactlyWhenItShould(
            UsageDataEventName name, UsageDataWorkTrackingSystem? system)
        {
            return EventsThatSayWhichKindOfSystem.Contains(name) == (system is not null);
        }

        /// <summary>
        /// Both ways round and both parts at once. The event that says a setting was switched must
        /// carry the setting and the direction; every other event must carry neither. Half of a
        /// switch is refused on either side, because read straight it would be counted as something
        /// nobody reported.
        /// </summary>
        private static bool NamesASwitchedSettingExactlyWhenItShould(
            UsageDataEventName name, UsageDataOptionalFeature? setting, bool? enabled)
        {
            if (EventsThatSayWhichSettingWasSwitched.Contains(name))
            {
                return setting is not null && enabled is not null;
            }

            return setting is null && enabled is null;
        }
    }
}
