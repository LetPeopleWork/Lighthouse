using System.Globalization;
using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Update;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ManualSorting
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5375 slice 03 — moving a Feature up the order.
    /// Backend-observable contract: one move command carrying identities places a Feature where another
    /// one stands, shifts only the block between the two, leaves everybody else's relative order alone,
    /// asks for a fresh forecast, and refuses anybody who may not write every Portfolio the Feature
    /// belongs to.
    /// </summary>
    public partial class Slice03RelativeMovesTest : ManualSortingAcceptanceTest
    {
        private const string ThisInstanceOwnsTheOrder = "ManualOrder";

        private readonly Dictionary<string, string> namesByReference = [];

        private readonly record struct ListedFeature(
            int Id,
            string Name,
            int Position,
            bool? CanMove,
            string? BlockReason,
            string[] BlockingPortfolios);

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

        /// <summary>
        /// A Feature that already holds a place. The only place a scenario may write a rank itself, and it
        /// exists for INV-O2's ragged set — gaps, repeats and Features nobody has placed are all legal.
        /// </summary>
        private int GivenAFeatureAlreadyPlacedAt(string name, int? place, string sourceOrder, params int[] portfolioIds)
        {
            var referenceId = $"FTR-PLACED-{name.Replace(' ', '-')}";
            namesByReference[referenceId] = name;
            return SeedFeature(name, referenceId, sourceOrder, place, StateCategories.ToDo, portfolioIds);
        }

        /// <summary>
        /// Seeds a run of Features the tracker ranked 1, 2, 3 … so that handing the order over gives them
        /// places in exactly that sequence. The fixture never writes the places it is about to assert.
        /// </summary>
        private int[] GivenTheTrackersOrderReads(int portfolioId, params string[] names)
            => GivenTheTrackersOrderReads(_ => [portfolioId], names);

        private int[] GivenTheTrackersOrderReads(Func<int, int[]> portfoliosForRow, params string[] names)
        {
            var ids = new int[names.Length];

            for (var row = 0; row < names.Length; row++)
            {
                ids[row] = GivenAFeatureTheTrackerRanked(
                    names[row],
                    $"FTR-{row + 1}",
                    (row + 1).ToString(CultureInfo.InvariantCulture),
                    portfoliosForRow(row));
            }

            return ids;
        }

        private int GivenTheresATeamWorkingOn(int portfolioId) => SeedTeamOn(portfolioId);

        private void GivenTheTeamHasWorkLeftOn(int featureId, int teamId, int remainingWorkItems)
            => SeedWorkOn(featureId, teamId, remainingWorkItems);

        /// <summary>
        /// A run chart that is not flat. AC-3.6 cannot be demonstrated on constant throughput at all:
        /// every Feature would finish on the same simulated day whatever order they were queued in.
        /// </summary>
        private void GivenTheTeamClosedItemsUnevenly(int teamId)
            => SeedThroughputFor(teamId, 1, 0, 3, 0, 2, 0, 0, 4, 1, 0, 2, 0, 0, 3, 1, 0, 0, 2, 5, 0);

        private void GivenTheCallerAdministersTheInstance() => TheCallerAdministersTheWholeInstance();

        private void GivenTheCallerMayWrite(params int[] portfolioIds) => TheCallerCanWritePortfolios(portfolioIds);

        private void GivenTheCallerMayOnlyRead(params int[] portfolioIds) => TheCallerCanReadPortfolios(portfolioIds);

        private void GivenTheCallerRunsOnePortfolioAndOnlyWatchesAnother(int runs, int watches)
            => TheCallerCanWriteSomePortfoliosAndOnlyReadOthers([runs], [watches]);

        private void GivenTheInstanceHasNoPremiumLicence() => TheInstanceIsNotLicensedForPremium();

        /// <summary>
        /// Hands the order to this instance, which places every Feature in the sequence the tracker had it
        /// in (INV-A3). Asserts its own success — a scenario that moved a Feature while the tracker still
        /// owned the order would be asserting nothing.
        /// </summary>
        private async Task GivenThisInstanceOwnsTheOrder()
        {
            TheCallerAdministersTheWholeInstance();

            var response = await SetOrderingPolicy(ThisInstanceOwnsTheOrder);

            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"Every move below rests on this instance owning the order. Body: {Excerpt(response.Body)}");

            // Handing the order over asks for a fresh forecast for every Portfolio - that is slice 02's
            // promise, and it would otherwise answer slice 03's question for it. Forgetting it here is what
            // makes "the MOVE asked for one" mean the move.
            ForecastUpdaterMock.Invocations.Clear();
        }

        private List<(string ReferenceId, int? ManualRank, string SourceOrder)> GivenTheOrderingColumnsAsStored()
            => ReadStoredOrderingColumns();

        // --- When ---

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerOpensTheFeaturesView() => GetAllFeatures();

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerTriesToPlaceItAbove(int featureId, int targetFeatureId)
            => MoveFeature(featureId, $"\"beforeFeatureId\":{targetFeatureId}");

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerTriesToPlaceItBelow(int featureId, int targetFeatureId)
            => MoveFeature(featureId, $"\"afterFeatureId\":{targetFeatureId}");

        /// <summary>D18's "Move to Bottom": no target at all, meaning the end of the order.</summary>
        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerTriesToSendItToTheBottom(int featureId)
            => MoveFeature(featureId, "\"beforeFeatureId\":null");

        private async Task WhenTheProductOwnerPlacesItAbove(int featureId, int targetFeatureId)
            => AssertTheMoveWasAccepted(await WhenTheProductOwnerTriesToPlaceItAbove(featureId, targetFeatureId));

        private async Task WhenTheProductOwnerPlacesItBelow(int featureId, int targetFeatureId)
            => AssertTheMoveWasAccepted(await WhenTheProductOwnerTriesToPlaceItBelow(featureId, targetFeatureId));

        private async Task WhenTheProductOwnerSendsItToTheBottom(int featureId)
            => AssertTheMoveWasAccepted(await WhenTheProductOwnerTriesToSendItToTheBottom(featureId));

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerSendsSomethingThatIsNotACommand(int featureId)
            => MoveFeatureWithBody(featureId, "[]");

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerNamesATargetThatIsNotAFeatureId(int featureId)
            => MoveFeature(featureId, "\"beforeFeatureId\":\"the top\"");

        private Task WhenAForecastRunsFor(int portfolioId) => DriveAForecastRun(portfolioId);

        private Task WhenTheTrackerSyncsWithItsOwnNewOrder(int portfolioId, params (string ReferenceId, string SourceOrder)[] rowsFromTheTracker)
        {
            var rows = rowsFromTheTracker
                .Select(row => (row.ReferenceId, Name: namesByReference.GetValueOrDefault(row.ReferenceId, row.ReferenceId), row.SourceOrder))
                .ToArray();

            return DriveAPortfolioRefresh(portfolioId, rows);
        }

        // --- Then ---

        private static void ThenTheOrderReads((HttpStatusCode Status, string Body) response, params string[] expectedNames)
        {
            var listed = ParseListedFeatures(response);

            Assert.That(listed.Select(f => f.Name).ToArray(), Is.EqualTo(expectedNames),
                $"Body: {Excerpt(response.Body)}");
        }

        /// <summary>
        /// AC-3.4 as a property, and the bounded-change contract's relative-order complement: for any pair
        /// of Features neither of which was moved, the order between them is what it was. This is the
        /// assertion both D4 and its slot-permutation fallback survive, which is what makes the fallback a
        /// re-scope rather than a rewrite.
        /// </summary>
        private static void ThenNobodyButTheMovedFeatureChangedPlacesWithAnybody(
            (HttpStatusCode Status, string Body) before,
            (HttpStatusCode Status, string Body) after,
            string theMovedFeature)
        {
            var was = ParseListedFeatures(before).Select(f => f.Name).Where(name => name != theMovedFeature).ToArray();
            var now = ParseListedFeatures(after).Select(f => f.Name).Where(name => name != theMovedFeature).ToArray();

            Assert.That(now, Is.EqualTo(was),
                "One Feature moved, so every other pair must read in the order it already read in (AC-3.4).");
        }

        private static void ThenEveryFeatureHoldsOnePlaceOfItsOwn((HttpStatusCode Status, string Body) response)
        {
            var positions = ParseListedFeatures(response).Select(f => f.Position).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(positions, Is.Not.Empty, $"The fixture must produce rows to judge. Body: {Excerpt(response.Body)}");
                Assert.That(positions, Has.All.GreaterThan(0), "Every Feature holds a place, including one nobody has placed yet.");
                Assert.That(positions.Distinct().Count(), Is.EqualTo(positions.Length),
                    $"Two Features claiming the same place in the order is not an order. Places: {string.Join(", ", positions)}");
                Assert.That(positions, Is.Ordered.Ascending, $"Places: {string.Join(", ", positions)}");
            }
        }

        private static void ThenTheRowSaysItMayNotBeMoved((HttpStatusCode Status, string Body) response, string featureName)
        {
            var row = ParseListedFeatures(response).Single(f => f.Name == featureName);

            Assert.That(row.CanMove, Is.False,
                $"The verdict is the server's to compute (ADR-136 SA-10) — a client that has to work it out gets it wrong. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheRowSaysItMayBeMoved((HttpStatusCode Status, string Body) response, string featureName)
        {
            var row = ParseListedFeatures(response).Single(f => f.Name == featureName);

            Assert.That(row.CanMove, Is.True,
                $"Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheReasonNamesThePortfolio((HttpStatusCode Status, string Body) response, string featureName, string portfolioName)
        {
            var row = ParseListedFeatures(response).Single(f => f.Name == featureName);

            Assert.That(row.BlockingPortfolios, Does.Contain(portfolioName),
                $"A refusal a Portfolio owner cannot act on is a dead end — name the one standing in the way (AC-3.8). Body: {Excerpt(response.Body)}");
        }

        /// <summary>
        /// SA-9 / ADR-136 §3. The tooltip is symmetric with what the row already discloses: a Portfolio the
        /// caller may not read is never named, so the refusal stays true without leaking who else exists.
        /// </summary>
        private static void ThenTheReasonNamesNoPortfolioTheCallerMayNotRead(
            (HttpStatusCode Status, string Body) response,
            string featureName,
            string theUnreadablePortfolio)
        {
            var row = ParseListedFeatures(response).Single(f => f.Name == featureName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.CanMove, Is.False, $"Body: {Excerpt(response.Body)}");
                Assert.That(row.BlockingPortfolios, Does.Not.Contain(theUnreadablePortfolio),
                    "Naming a Portfolio the caller may not read would tell them it exists (SA-9).");
                Assert.That(row.BlockReason, Is.Not.Null.And.Not.Empty,
                    "A refusal that names nobody must still say something true, or the button is disabled for no stated reason.");
            }
        }

        private static void ThenTheInstanceRefusesTheCaller((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.Forbidden),
                $"A move nobody may make is refused, not silently dropped. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheInstanceRefusesForWantOfALicence((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.Forbidden),
                $"Owning the order is premium (S11/D12), and so is changing it. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheInstanceCannotMakeSenseOfTheMove((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.BadRequest),
                $"The command carries exactly one target (DDD-7). Two, or a target that does not exist, is not a move. Body: {Excerpt(response.Body)}");
        }

        /// <summary>
        /// Deliberately weaker than a status assertion. Whether a target that is not there reads as a bad
        /// request or as a missing one is not something the acceptance criteria decide; that it did not
        /// report success, and did not fall over, is.
        /// </summary>
        private static void ThenTheMoveDidNotSucceed((HttpStatusCode Status, string Body) response)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Status, Is.Not.EqualTo(HttpStatusCode.OK),
                    $"A move against a target that does not exist must not report success. Body: {Excerpt(response.Body)}");
                Assert.That((int)response.Status, Is.LessThan(500),
                    $"...and it is the caller's mistake, not the instance falling over. Body: {Excerpt(response.Body)}");
            }
        }

        private void ThenTheTrackersOwnValuesAreUnchangedFrom(List<(string ReferenceId, int? ManualRank, string SourceOrder)> before)
        {
            var after = ReadStoredOrderingColumns();

            Assert.That(after.Select(row => (row.ReferenceId, row.SourceOrder)).ToArray(),
                Is.EqualTo(before.Select(row => (row.ReferenceId, row.SourceOrder)).ToArray()),
                "A move writes places and nothing else — the tracker's own value is byte-identical for every Feature (D5).");
        }

        private void ThenAFreshForecastWasAskedForFor(params int[] portfolioIds)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var portfolioId in portfolioIds)
                {
                    ForecastUpdaterMock.Verify(
                        updater => updater.TriggerUpdate(portfolioId),
                        Times.AtLeastOnce,
                        "A move that leaves the dates stale is the one failure indistinguishable from success (ADR-133).");
                }
            }
        }

        private void ThenNoFreshForecastWasAskedFor()
        {
            ForecastUpdaterMock.Verify(
                updater => updater.TriggerUpdate(It.IsAny<int>()),
                Times.Never,
                "A refused move changed nothing, so it must not spend a forecast run saying so.");
        }

        /// <summary>
        /// AC-3.6. Not "the dates changed" — which a re-run would satisfy on its own through simulation
        /// noise — but that the Feature brought forward finished sooner and the one it displaced later.
        /// </summary>
        private static void ThenTheDateMovedEarlierFor(
            (HttpStatusCode Status, string Body) before,
            (HttpStatusCode Status, string Body) after,
            string featureName)
        {
            var was = ParseEightyFivePercentDate(before, featureName);
            var now = ParseEightyFivePercentDate(after, featureName);

            Assert.That(now, Is.LessThan(was),
                $"'{featureName}' was placed at the front of the queue, so its 85% date must come forward. Was {was:yyyy-MM-dd}, now {now:yyyy-MM-dd}.");
        }

        private static void ThenTheDateMovedLaterFor(
            (HttpStatusCode Status, string Body) before,
            (HttpStatusCode Status, string Body) after,
            string featureName)
        {
            var was = ParseEightyFivePercentDate(before, featureName);
            var now = ParseEightyFivePercentDate(after, featureName);

            Assert.That(now, Is.GreaterThan(was),
                $"'{featureName}' was displaced, so its 85% date must slip. Was {was:yyyy-MM-dd}, now {now:yyyy-MM-dd}.");
        }

        // --- Parsing ---

        private static void AssertTheMoveWasAccepted((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The move port must accept a move the caller may make — every assertion below rests on it. Body: {Excerpt(response.Body)}");
        }

        private static DateTime ParseEightyFivePercentDate((HttpStatusCode Status, string Body) response, string featureName)
        {
            using var document = JsonDocument.Parse(GuardedBody(response));

            var forecasts = document.RootElement
                .EnumerateArray()
                .Single(element => element.GetProperty("name").GetString() == featureName)
                .GetProperty("forecasts");

            var eightyFive = forecasts
                .EnumerateArray()
                .Where(forecast => forecast.GetProperty("probability").GetInt32() == 85)
                .Select(forecast => forecast.GetProperty("expectedDate").GetDateTime())
                .ToList();

            Assert.That(eightyFive, Is.Not.Empty,
                $"'{featureName}' reports no 85% date, so the fixture proves nothing about sequencing. A Feature with work left on a team with throughput must be forecast.");

            return eightyFive.Max();
        }

        private static List<ListedFeature> ParseListedFeatures((HttpStatusCode Status, string Body) response)
        {
            using var document = JsonDocument.Parse(GuardedBody(response));

            return document.RootElement
                .EnumerateArray()
                .Select(element => new ListedFeature(
                    element.GetProperty("id").GetInt32(),
                    element.GetProperty("name").GetString() ?? string.Empty,
                    element.GetProperty("position").GetInt32(),
                    element.TryGetProperty("canMove", out var canMove) && canMove.ValueKind != JsonValueKind.Null
                        ? canMove.GetBoolean()
                        : null,
                    element.TryGetProperty("moveBlockReason", out var reason) && reason.ValueKind == JsonValueKind.String
                        ? reason.GetString()
                        : null,
                    element.TryGetProperty("blockingPortfolios", out var blocking) && blocking.ValueKind == JsonValueKind.Array
                        ? [.. blocking.EnumerateArray().Select(portfolio => portfolio.GetProperty("name").GetString() ?? string.Empty)]
                        : []))
                .ToList();
        }

        private static string GuardedBody((HttpStatusCode Status, string Body) response)
        {
#pragma warning disable NUnit2045 // Guard-then-parse, not independent asserts: under Assert.Multiple the JSON parse would run on a failed response and throw over the clear message.
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The Features view read port must answer. Body: {Excerpt(response.Body)}");
            Assert.That(response.Body.TrimStart(), Does.StartWith("["),
                $"The read port must return a JSON array, not HTML/other. Body starts: {Excerpt(response.Body)}");
#pragma warning restore NUnit2045

            return response.Body;
        }

        private static string Excerpt(string body) => body[..Math.Min(160, body.Length)];
    }
}
