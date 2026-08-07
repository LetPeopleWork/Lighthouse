using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ManualSorting
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5375 slice 02 — the instance takes ownership of
    /// the order. Backend-observable contract: one switch decides whether the order shown, forecast and
    /// listed comes from the tracker or from this instance; flipping it moves nobody; the tracker keeps
    /// writing its own value and is ignored; flipping it back gives the tracker's sequence straight back
    /// while the places this instance chose are kept.
    /// </summary>
    public partial class Slice02ManualRankTest : ManualSortingAcceptanceTest
    {
        private const string ThisInstanceOwnsTheOrder = "ManualOrder";
        private const string TheTrackerOwnsTheOrder = "SourceOrder";

        private readonly Dictionary<string, string> namesByReference = [];

        private readonly record struct ListedFeature(int Id, string Name, int Position);

        // --- Given ---

        private int GivenAPortfolio(string name) => SeedPortfolio(name);

        private int GivenAFeatureTheTrackerRanked(string name, string referenceId, string sourceOrder, params int[] portfolioIds)
        {
            namesByReference[referenceId] = name;
            return SeedFeature(name, referenceId, sourceOrder, manualRank: null, StateCategories.ToDo, portfolioIds);
        }

        private int GivenAFinishedFeature(string name, string referenceId, string sourceOrder, params int[] portfolioIds)
        {
            namesByReference[referenceId] = name;
            return SeedFeature(name, referenceId, sourceOrder, manualRank: null, StateCategories.Done, portfolioIds);
        }

        private int GivenAFeatureTheTrackerNeverRanked(string name, string referenceId, params int[] portfolioIds)
            => GivenAFeatureTheTrackerRanked(name, referenceId, string.Empty, portfolioIds);

        /// <summary>
        /// A run of Features carrying one connector's <c>Order</c> shape, in the shape's own arbitrary
        /// sequence — the fixture never states the order it expects them to come back in, only the values
        /// the tracker wrote.
        /// </summary>
        private void GivenFeaturesTheTrackerRanked(int portfolioId, params string[] sourceOrders)
        {
            for (var index = 0; index < sourceOrders.Length; index++)
            {
                GivenAFeatureTheTrackerRanked($"Feature seeded {index + 1}", $"FTR-SEED-{index + 1}", sourceOrders[index], portfolioId);
            }
        }

        /// <summary>
        /// A Feature that already holds a place — the precondition for INV-O2's ragged set, and the only
        /// place a scenario is allowed to write a rank itself.
        /// </summary>
        private int GivenAFeatureAlreadyPlacedAt(string name, int place, string sourceOrder, params int[] portfolioIds)
        {
            var referenceId = $"FTR-PLACED-{place}-{name.GetHashCode(StringComparison.Ordinal)}";
            namesByReference[referenceId] = name;
            return SeedFeature(name, referenceId, sourceOrder, place, StateCategories.ToDo, portfolioIds);
        }

        private int GivenTheresATeamWorkingOn(int portfolioId) => SeedTeamOn(portfolioId);

        private void GivenTheTeamHasWorkLeftOn(int featureId, int teamId) => SeedWorkOn(featureId, teamId);

        private void GivenTheCallerAdministersTheInstance() => TheCallerAdministersTheWholeInstance();

        private void GivenTheCallerMayWriteOnly(params int[] portfolioIds) => TheCallerCanWritePortfolios(portfolioIds);

        private void GivenTheInstanceHasNoPremiumLicence() => TheInstanceIsNotLicensedForPremium();

        private List<(string ReferenceId, int? ManualRank, string SourceOrder)> GivenTheOrderingColumnsAsStored()
            => ReadStoredOrderingColumns();

        // --- When ---

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerOpensTheFeaturesView() => GetAllFeatures();

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerOpensThePortfolio(int portfolioId) => GetPortfolio(portfolioId);

        private Task<(HttpStatusCode Status, string Body)> WhenAnyoneAsksWhoOwnsTheOrder() => GetOrderingPolicy();

        private Task<(HttpStatusCode Status, string Body)> WhenTheConfigAdminTriesToHandTheOrderOver() => SetOrderingPolicy(ThisInstanceOwnsTheOrder);

        /// <summary>
        /// Handing the order over is a precondition in most scenarios, so it asserts its own success —
        /// a scenario that silently ran on a refused switch would assert nothing at all.
        /// </summary>
        private async Task WhenTheConfigAdminHandsTheOrderOver()
        {
            var response = await SetOrderingPolicy(ThisInstanceOwnsTheOrder);
            AssertTheSwitchWasAccepted(response, ThisInstanceOwnsTheOrder);
        }

        private async Task WhenTheConfigAdminGivesTheOrderBack()
        {
            var response = await SetOrderingPolicy(TheTrackerOwnsTheOrder);
            AssertTheSwitchWasAccepted(response, TheTrackerOwnsTheOrder);
        }

        /// <summary>
        /// One real refresh, with the tracker handing back its own new order for the same Features it
        /// sent last time. Names come from what the tracker already called them, so a scenario reads
        /// the sequence by name rather than by reference id.
        /// </summary>
        private Task WhenTheTrackerSyncsWithItsOwnNewOrder(int portfolioId, params (string ReferenceId, string SourceOrder)[] rowsFromTheTracker)
        {
            var rows = rowsFromTheTracker
                .Select(row => (row.ReferenceId, Name: namesByReference.GetValueOrDefault(row.ReferenceId, row.ReferenceId), row.SourceOrder))
                .ToArray();

            return DriveAPortfolioRefresh(portfolioId, rows);
        }

        // --- Then ---

        private static void ThenTheListIsUnchanged((HttpStatusCode Status, string Body) before, (HttpStatusCode Status, string Body) after)
        {
            var wasListed = ParseListedFeatures(before);
            var isListed = ParseListedFeatures(after);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isListed.Select(f => f.Name).ToArray(), Is.EqualTo(wasListed.Select(f => f.Name).ToArray()),
                    "Nobody may move. A switch that reshuffles the list on the way in is indistinguishable from a bug.");
                Assert.That(isListed.Select(f => f.Position).ToArray(), Is.EqualTo(wasListed.Select(f => f.Position).ToArray()),
                    "Every row must keep the place it held, not merely its neighbours.");
            }
        }

        private static void ThenTheListReads((HttpStatusCode Status, string Body) response, string[] expectedNames)
        {
            var listed = ParseListedFeatures(response);

            Assert.That(listed.Select(f => f.Name).ToArray(), Is.EqualTo(expectedNames),
                $"Body: {Excerpt(response.Body)}");
        }

        /// <summary>
        /// K4. The Features view and the Portfolio detail are two different sorts of two different sets;
        /// the only thing that makes them one order is that both go through the same selection point.
        /// </summary>
        private static void ThenBothWaysInAgreeOnTheOrder(
            (HttpStatusCode Status, string Body) throughTheFeaturesView,
            (HttpStatusCode Status, string Body) throughThePortfolio)
        {
            var throughThePortfolioNames = ParsePortfolioFeatureNames(throughThePortfolio);
            var throughTheFeaturesViewNames = ParseListedFeatures(throughTheFeaturesView)
                .Select(f => f.Name)
                .Where(throughThePortfolioNames.Contains)
                .ToArray();

            Assert.That(throughThePortfolioNames, Is.EqualTo(throughTheFeaturesViewNames),
                "Both ways in must read off one selection point, or two people looking at the same instance are looking at two different orders.");
        }

        private static void ThenEveryFeatureHoldsOnePlaceOfItsOwn((HttpStatusCode Status, string Body) response)
        {
            var positions = ParseListedFeatures(response).Select(f => f.Position).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(positions, Is.Not.Empty, $"The fixture must produce rows to judge. Body: {Excerpt(response.Body)}");
                Assert.That(positions, Has.All.GreaterThan(0), "Every Feature holds a place, including one nobody has placed yet.");
                Assert.That(positions.Distinct().Count(), Is.EqualTo(positions.Length),
                    $"Gaps and repeats among the places stored are legal; two Features claiming the same place in the order is not. Places: {string.Join(", ", positions)}");
                Assert.That(positions, Is.Ordered.Ascending, $"Places: {string.Join(", ", positions)}");
            }
        }

        private void ThenTheTrackerStillOwnsItsOwnOrderValues(string referenceId, string expectedSourceOrder)
        {
            var stored = ReadStoredOrderingColumns().Single(row => row.ReferenceId == referenceId);

            Assert.That(stored.SourceOrder, Is.EqualTo(expectedSourceOrder),
                "The tracker's own value keeps being written on every sync (D5) — the two fields are independent, and only the comparison changed.");
        }

        private void ThenTheStoredPlacesAreUnchangedFrom(List<(string ReferenceId, int? ManualRank, string SourceOrder)> before)
        {
            var after = ReadStoredOrderingColumns();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(before.Select(row => row.ManualRank), Has.All.Not.Null,
                    "The precondition itself: handing the order over must have given every Feature a place, or this scenario proves nothing.");
                Assert.That(after.Select(row => (row.ReferenceId, row.ManualRank)).ToArray(),
                    Is.EqualTo(before.Select(row => (row.ReferenceId, row.ManualRank)).ToArray()),
                    "Giving the order back keeps the places this instance chose, so turning it on again is a return rather than a re-reading (D9).");
            }
        }

        private void ThenTheTrackersOwnValuesAreUnchangedFrom(List<(string ReferenceId, int? ManualRank, string SourceOrder)> before)
        {
            var after = ReadStoredOrderingColumns();

            Assert.That(after.Select(row => (row.ReferenceId, row.SourceOrder)).ToArray(),
                Is.EqualTo(before.Select(row => (row.ReferenceId, row.SourceOrder)).ToArray()),
                "Taking the order over writes places and nothing else — the tracker's own value is byte-identical for every Feature (D5).");
        }

        private static void ThenTheTrackerOwnsTheOrder((HttpStatusCode Status, string Body) response)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                    $"Asking who owns the order must answer even before anybody has chosen. Body: {Excerpt(response.Body)}");
                Assert.That(response.Body.TrimStart(), Does.Not.StartWith("<"),
                    $"The read port must answer, not fall through to the single-page app — the port appears unimplemented. Body starts: {Excerpt(response.Body)}");
                Assert.That(response.Body, Does.Contain(TheTrackerOwnsTheOrder),
                    $"An instance where nobody has chosen follows the tracker, and says so without a row having to exist. Body: {Excerpt(response.Body)}");
            }
        }

        private static void ThenTheInstanceRefusesForWantOfALicence((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.Forbidden),
                $"Handing the order over is premium (S11), and a refusal is not a silent no-op. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheInstanceRefusesTheCaller((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.Forbidden),
                $"Who owns the order is an instance-wide decision, so it takes an instance administrator. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheViewIsStillReachable((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The view is not premium and stays open however the ordering question is answered (D12). Body: {Excerpt(response.Body)}");
        }

        // --- Parsing ---

        private static void AssertTheSwitchWasAccepted((HttpStatusCode Status, string Body) response, string policy)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The ordering switch must accept '{policy}' — every scenario below rests on it having been taken. Body: {Excerpt(response.Body)}");
        }

        private static List<ListedFeature> ParseListedFeatures((HttpStatusCode Status, string Body) response)
        {
#pragma warning disable NUnit2045 // Guard-then-parse, not independent asserts: under Assert.Multiple the JSON parse below would run on a failed response and throw over the clear message.
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The Features view read port must answer. Body: {Excerpt(response.Body)}");
            Assert.That(response.Body.TrimStart(), Does.StartWith("["),
                $"The read port must return a JSON array, not HTML/other. Body starts: {Excerpt(response.Body)}");
#pragma warning restore NUnit2045

            using var document = JsonDocument.Parse(response.Body);

            return document.RootElement
                .EnumerateArray()
                .Select(element => new ListedFeature(
                    element.GetProperty("id").GetInt32(),
                    element.GetProperty("name").GetString() ?? string.Empty,
                    element.GetProperty("position").GetInt32()))
                .ToList();
        }

        private static string[] ParsePortfolioFeatureNames((HttpStatusCode Status, string Body) response)
        {
#pragma warning disable NUnit2045 // Guard-then-parse: the JSON read below would throw over the clear message under Assert.Multiple.
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The Portfolio detail read port must answer. Body: {Excerpt(response.Body)}");
            Assert.That(response.Body.TrimStart(), Does.StartWith("{"),
                $"The Portfolio detail read port must return an object. Body starts: {Excerpt(response.Body)}");
#pragma warning restore NUnit2045

            using var document = JsonDocument.Parse(response.Body);

            return document.RootElement
                .GetProperty("features")
                .EnumerateArray()
                .Select(element => element.GetProperty("name").GetString() ?? string.Empty)
                .ToArray();
        }

        private static string Excerpt(string body) => body[..Math.Min(120, body.Length)];
    }
}
