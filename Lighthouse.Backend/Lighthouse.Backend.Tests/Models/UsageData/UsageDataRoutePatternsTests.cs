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
