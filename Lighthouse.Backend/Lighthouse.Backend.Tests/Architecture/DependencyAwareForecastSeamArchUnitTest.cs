using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.Integration.Containers;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class DependencyAwareForecastSeamArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string TheUpdateComponents =
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update";

        private const string TheOnlyOneThatMayForecast = "ForecastUpdater";

        [Test]
        public void OnlyTheForecastUpdater_ReachesForTheForecastService()
        {
            Types().That()
                .ResideInNamespace(TheUpdateComponents).And()
                .DoNotHaveFullNameContaining(TheOnlyOneThatMayForecast)
                .Should().NotDependOnAny(Types().That().Are(typeof(IForecastService)))
                .Because(
                    "Only the forecast updater may start a forecast among the update components. It is the one " +
                    "caller the admission check can see, and that check is what keeps a portfolio forecast once " +
                    "per refresh round. A second component here reaching for the forecast service would forecast " +
                    "behind that check, and one refresh round would leave the same portfolio holding two " +
                    "different delivery dates.")
                .Check(Architecture);
        }

        [Test]
        public void EveryStoreThatRecordsWorkInFlight_IsComparedAgainstTheOthers()
        {
            var storesInProduction = typeof(IUpdateStatusStore).Assembly.GetTypes()
                .Where(candidate => candidate.IsClass && !candidate.IsAbstract)
                .Where(typeof(IUpdateStatusStore).IsAssignableFrom)
                .ToArray();

            var storesCompared = UpdateStatusStoreContainerTests.StoresComparedAgainstEachOther
                .Select(entry => entry.Store)
                .ToArray();

            Assert.That(storesInProduction, Is.EquivalentTo(storesCompared),
                "Every store that records which updates are in flight has to be compared against the others, " +
                "because they must answer the queued-work question identically. That comparison names the " +
                "stores it runs by hand, so a store missing from that list is a store nobody compares: it could " +
                "quietly answer differently, and the same portfolio would then forecast at a different moment " +
                "purely because of how Lighthouse is deployed. Add the new store to " +
                nameof(UpdateStatusStoreContainerTests.StoresComparedAgainstEachOther) + " in " +
                nameof(UpdateStatusStoreContainerTests) + ", which is the list that comparison runs.");
        }
    }
}
