using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Acceptance scenarios - Slice 03: everything the earlier slices delivered, on every work tracking
    /// system that can express a dependency.
    ///
    /// One scenario body, run once per tracker, rather than a copy per tracker. That is the claim worth
    /// testing: a dependency is read by a connector and then handled by code that has never heard of a
    /// connector, so adding a fourth tracker should be a mapper and nothing else. If a tracker needed its
    /// own version of a scenario below, the mapping would be in the wrong place, and this fixture is
    /// where that would show.
    ///
    /// Each case drives the tracker's real connector over a payload in that tracker's own shape, so what
    /// reaches the store is what a refresh would have had in hand - not a Feature written out by a test.
    /// </summary>
    [TestFixtureSource(typeof(TrackerPayloadSources), nameof(TrackerPayloadSources.EveryTrackerThatCanExpressADependency))]
    [Category("acceptance")]
    [Category("epic-4365-dependencies")]
    [Category("slice-03")]
    public class Slice03ConnectorParityScenarios(DependenciesAcceptanceTest.ITrackerPayloadSource tracker) : DependenciesAcceptanceTest
    {
        private const string Checkout = "Checkout redesign";
        private const string Payment = "Payment gateway upgrade";
        private const string Warehouse = "Warehouse sync";

        [SetUp]
        public void UseThisTracker() => TheTracker = tracker;

        // @driving_adapter @us-09 - "Everything the earlier slices delivered behaves the same on every
        // tracker". The row's count, the named entries, and the one warning that stands against an entry
        // outside this Portfolio.
        [Test]
        public async Task Everything_the_earlier_slices_delivered_reads_the_same_on_this_tracker()
        {
            var platform = SeedPortfolio("Platform");
            var warehouse = SeedPortfolio("Warehouse");

            await DriveAPortfolioRefresh(
                platform,
                Row(Payment),
                Row(Checkout, waitingOn: [Payment, Warehouse]));
            await DriveAPortfolioRefresh(warehouse, Row(Warehouse));

            var checkout = await TheRowFor(Checkout);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(EntriesOf(checkout), Has.Count.EqualTo(2), "The row says how many Features it waits on.");
                Assert.That(NamesIn(checkout), Is.EquivalentTo(new[] { Payment, Warehouse }), "Each entry names the Feature it points at.");
                Assert.That(LinksIn(checkout), Has.All.Not.Empty, "Each entry leads to the Feature it points at, in the tracker it came from.");
                Assert.That(ReasonAgainst(checkout, Warehouse), Is.EqualTo("OutsideThisPortfolio"));
                Assert.That(ReasonAgainst(checkout, Payment), Is.Null, "A dependency there is nothing wrong with carries no reason at all.");
            }
        }

        [Test]
        public async Task A_feature_waiting_on_nothing_says_so_on_this_tracker_too()
        {
            var platform = SeedPortfolio("Platform");

            await DriveAPortfolioRefresh(platform, Row(Payment), Row(Checkout));

            Assert.That(EntriesOf(await TheRowFor(Checkout)), Is.Empty);
        }

        // A link naming something this instance does not hold cannot be named to a reader. Every tracker
        // has to behave the same here, because every tracker can point at something outside the query.
        [Test]
        public async Task A_link_naming_nothing_lighthouse_holds_is_not_on_the_row_on_this_tracker_either()
        {
            var platform = SeedPortfolio("Platform");

            await DriveAPortfolioRefresh(
                platform,
                Row(Payment),
                Row(Checkout, waitingOn: [Payment, "A Feature nobody imported"]));

            Assert.That(NamesIn(await TheRowFor(Checkout)), Is.EquivalentTo(new[] { Payment }));
        }

        [Test]
        public async Task A_feature_waiting_on_itself_is_reported_as_a_circle_on_this_tracker_too()
        {
            var platform = SeedPortfolio("Platform");

            await DriveAPortfolioRefresh(platform, Row(Checkout, waitingOn: [Checkout]));

            Assert.That(ReasonAgainst(await TheRowFor(Checkout), Checkout), Is.EqualTo("InALoop"));
        }

        /// <summary>
        /// A row as the scenario means it: reference ids are whatever this tracker calls things, which is
        /// a number on Azure DevOps, a key on Jira and the id of a Project on Linear.
        /// </summary>
        private TrackedFeature Row(string name, string[]? waitingOn = null)
            => new(
                tracker.ReferenceIdFor(name),
                name,
                Array.ConvertAll(waitingOn ?? [], tracker.ReferenceIdFor));

        private async Task<JsonElement> TheRowFor(string name)
            => await ReadTheFeatureThePayloadCarries(tracker.ReferenceIdFor(name))
                ?? throw new InvalidOperationException($"The Features view carries no row for {name}.");

        private static List<JsonElement> EntriesOf(JsonElement row)
            => [.. row.GetProperty("dependsOn").EnumerateArray()];

        private static List<string> NamesIn(JsonElement row)
            => EntriesOf(row).ConvertAll(entry => entry.GetProperty("name").GetString() ?? string.Empty);

        private static List<string> LinksIn(JsonElement row)
            => EntriesOf(row).ConvertAll(entry => entry.GetProperty("url").GetString() ?? string.Empty);

        private string? ReasonAgainst(JsonElement row, string name)
        {
            var referenceId = tracker.ReferenceIdFor(name);

            foreach (var entry in EntriesOf(row))
            {
                if (entry.GetProperty("referenceId").GetString() != referenceId)
                {
                    continue;
                }

                return entry.TryGetProperty("notHonouredReason", out var reason) && reason.ValueKind != JsonValueKind.Null
                    ? reason.GetString()
                    : null;
            }

            throw new InvalidOperationException($"The row names no entry for {name}.");
        }
    }
}
