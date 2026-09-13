using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Tests.Models.UsageData
{
    /// <summary>
    /// The browser names a page by choosing from a list; this is where that choice is turned into the
    /// address a third party is shown. Both halves matter: a choice nobody wrote an address for would
    /// arrive somewhere as nothing, and an address with a number in it is how a customer's Team or
    /// Portfolio would end up outside Lighthouse after all the care taken to keep it off the wire.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataRoutePatternsTests
    {
        /// <summary>
        /// The address written here is the whole of what a third party is shown about where somebody
        /// was - it replaces the real one, which carries a customer's own Team or Portfolio in it.
        /// Every one of these therefore has to be pinned to the page it stands for: a blank leaves an
        /// event naming nowhere, and one silently pointing at a different page makes the figures a
        /// report about somewhere nobody went.
        /// </summary>
        [TestCase(UsageDataRouteKey.TeamDetail_Features, "/teams/:id/features")]
        [TestCase(UsageDataRouteKey.TeamDetail_Forecasts, "/teams/:id/forecasts")]
        [TestCase(UsageDataRouteKey.TeamDetail_Metrics, "/teams/:id/metrics")]
        [TestCase(UsageDataRouteKey.TeamDetail_Settings, "/teams/:id/settings")]
        [TestCase(UsageDataRouteKey.TeamDetail_Access, "/teams/:id/access")]
        [TestCase(UsageDataRouteKey.PortfolioDetail_Features, "/portfolios/:id/features")]
        [TestCase(UsageDataRouteKey.PortfolioDetail_Metrics, "/portfolios/:id/metrics")]
        [TestCase(UsageDataRouteKey.PortfolioDetail_Deliveries, "/portfolios/:id/deliveries")]
        [TestCase(UsageDataRouteKey.PortfolioDetail_Settings, "/portfolios/:id/settings")]
        [TestCase(UsageDataRouteKey.PortfolioDetail_Access, "/portfolios/:id/access")]
        public void APageTheBrowserNamed_IsPublishedAsTheAddressStandingInForIt(
            UsageDataRouteKey named, string published)
        {
            Assert.That(UsageDataRoutePatterns.All[named], Is.EqualTo(published),
                "the address published for this page is not the one it stands for, so every event "
                + "reported for it is counted somewhere else - or, if it is blank, nowhere at all");
        }

        [Test]
        public void EveryPageTheBrowserCanName_PublishesAnAddressWithNoIdentifierInIt()
        {
            var withoutAnAddress = Enum.GetValues<UsageDataRouteKey>()
                .Where(key => !UsageDataRoutePatterns.All.ContainsKey(key))
                .Select(key => key.ToString())
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            var carryingANumber = UsageDataRoutePatterns.All
                .Where(entry => entry.Value.Any(char.IsDigit))
                .Select(entry => $"{entry.Key} -> {entry.Value}")
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutAnAddress, Is.Empty,
                    "a page the browser is allowed to name has no address written down for it, so "
                    + "reporting it would arrive with nowhere attached. Found: "
                    + string.Join(", ", withoutAnAddress));
                Assert.That(carryingANumber, Is.Empty,
                    "an address here is a shape, never a place - a digit means somebody wrote down a "
                    + "particular Team or Portfolio, which is the one thing this path promises never "
                    + "to carry. Found: " + string.Join(", ", carryingANumber));
            }
        }
    }
}
