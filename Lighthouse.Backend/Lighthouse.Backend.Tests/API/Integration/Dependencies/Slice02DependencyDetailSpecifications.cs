using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;
using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Step definitions for the second dependency slice. Backend-observable contract: one entry for every
    /// Feature this Lighthouse holds that a Feature waits on, each naming that Feature, its state, the
    /// Portfolios it belongs to and the record it came from - read over the real route, never off the store.
    /// </summary>
    public partial class Slice02DependencyDetailTest : DependenciesAcceptanceTest
    {
        // --- Given ---

        private int GivenAPortfolio(string name) => SeedPortfolio(name);

        private static TrackedFeature AFeatureTheTrackerHolds(string referenceId, string name)
            => new(referenceId, name, []);

        private static TrackedFeature AFeatureWaitingOn(string referenceId, string name, string[] waitsOn)
            => new(referenceId, name, waitsOn);

        /// <summary>
        /// A refresh that also hands back the two Features every scenario here points at, so a scenario
        /// about the waiting end says nothing about the far end being there.
        /// </summary>
        private Task GivenARefreshedPortfolio(int portfolioId, TrackedFeature theFeatureUnderTest)
            => DriveAPortfolioRefresh(
                portfolioId,
                AFeatureTheTrackerHolds("F-1", "Rebuild the search index"),
                AFeatureTheTrackerHolds("F-2", "Retire the legacy importer"),
                theFeatureUnderTest);

        private void GivenNoPremiumLicence()
            => LicenseServiceMock.Setup(licences => licences.CanUsePremiumFeatures()).Returns(false);

        // --- When ---

        private async Task<List<JsonElement>> WhenTheReaderOpensWhatItWaitsOn(string featureReferenceId)
        {
            var (status, entries) = await AskFor(Client, "latest", featureReferenceId);

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK),
                $"The reader must be handed what {featureReferenceId} waits on before anything can be said about it.");

            return entries;
        }

        private async Task<(string VersionOne, string Latest)> WhenTheReaderOpensItOnBothVersions(string featureReferenceId)
        {
            var featureId = TheFeatureIdOf(featureReferenceId);

            return (await ReadTheBodyOf(Client, "v1", featureId), await ReadTheBodyOf(Client, "latest", featureId));
        }

        private async Task<HttpStatusCode> WhenAReaderOfAnotherPortfolioOpensWhatItWaitsOn(
            string featureReferenceId, int theOnlyPortfolioTheyCanRead)
        {
            var featureId = TheFeatureIdOf(featureReferenceId);

            using var reader = Factory.CreateClient().AsPortfolioViewer(theOnlyPortfolioTheyCanRead);
            using var response = await reader.GetAsync($"/api/latest/features/{featureId}/dependencies");

            return response.StatusCode;
        }

        // --- Then ---

        private static void ThenTheListIs(List<JsonElement> entries, params string[] expectedReferenceIds)
        {
            var named = entries.Select(TheReferenceIdOn).Order().ToArray();

            Assert.That(named, Is.EqualTo(expectedReferenceIds.Order().ToArray()),
                $"The list must name exactly the Features waited on that Lighthouse holds. Listed: {string.Join(", ", named)}");
        }

        private static DependencyEntry ThenTheEntryFor(List<JsonElement> entries, string referenceId)
        {
            var entry = entries.SingleOrDefault(candidate => TheReferenceIdOn(candidate) == referenceId);

            Assert.That(entry.ValueKind, Is.EqualTo(JsonValueKind.Object),
                $"There must be exactly one entry for {referenceId} to be judged.");

            return new DependencyEntry(entry, referenceId);
        }

        /// <summary>
        /// The number on the row and the length of the list under it are read seconds apart by the same
        /// person. They are worked out in two places, so nothing but this makes them agree.
        /// </summary>
        private async Task ThenTheListIsAsLongAsTheCountOnTheRow(string featureReferenceId)
        {
            var row = await ReadTheFeatureThePayloadCarries(featureReferenceId)
                ?? throw new InvalidOperationException($"The payload carried no {featureReferenceId} for its count to be judged.");
            var entries = await WhenTheReaderOpensWhatItWaitsOn(featureReferenceId);

            Assert.That(entries, Has.Count.EqualTo(row.GetProperty("dependsOnCount").GetInt32()),
                $"The list must account for the number on the row, entry for entry. Row: {row}");
        }

        private static void ThenBothVersionsSaidTheSameThing(string versionOne, string latest)
        {
            Assert.That(versionOne, Is.EqualTo(latest),
                "A client pinned to a version must be able to ask for this at all, and must be told the same thing.");
        }

        private static void ThenTheAnswerIsNotFound(HttpStatusCode answer)
        {
            Assert.That(answer, Is.EqualTo(HttpStatusCode.NotFound),
                "A Feature the reader may not read must answer exactly as it does when there is no such Feature.");
        }

        // --- Reading the route ---

        private async Task<(HttpStatusCode Status, List<JsonElement> Entries)> AskFor(
            HttpClient client, string version, string featureReferenceId)
        {
            var featureId = TheFeatureIdOf(featureReferenceId);

            using var response = await client.GetAsync($"/api/{version}/features/{featureId}/dependencies");
            if (!response.IsSuccessStatusCode)
            {
                return (response.StatusCode, []);
            }

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return (response.StatusCode, payload.RootElement.EnumerateArray().Select(entry => entry.Clone()).ToList());
        }

        private static async Task<string> ReadTheBodyOf(HttpClient client, string version, int featureId)
        {
            using var response = await client.GetAsync($"/api/{version}/features/{featureId}/dependencies");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private static string? TheReferenceIdOn(JsonElement entry)
            => entry.TryGetProperty("referenceId", out var referenceId) ? referenceId.GetString() : null;

        /// <summary>
        /// One entry, kept beside the id it was found by so every failure says which entry disappointed.
        /// </summary>
        private readonly record struct DependencyEntry(JsonElement Json, string ReferenceId)
        {
            public void Names(string expectedName)
                => Assert.That(TextOf("name"), Is.EqualTo(expectedName),
                    $"An entry has to name the Feature being waited on, or the reader is looking at an id. Entry: {Json}");

            public void SaysItIsInState(string expectedState)
                => Assert.That(TextOf("state"), Is.EqualTo(expectedState),
                    $"Whether the wait is nearly over is the state of the Feature waited on. Entry: {Json}");

            public void SaysItBelongsTo(string expectedPortfolio)
                => Assert.That(PortfolioNames(), Does.Contain(expectedPortfolio),
                    $"Which Portfolios the Feature belongs to is who the reader has to go and talk to. Entry: {Json}");

            public void OffersAWayToOpenIt(string expectedUrl)
                => Assert.That(TextOf("url"), Is.EqualTo(expectedUrl),
                    $"Deciding what to do about a wait happens in the work tracking system, so the entry has to lead there. Entry: {Json}");

            private string? TextOf(string property)
                => Json.TryGetProperty(property, out var value) ? value.GetString() : null;

            private List<string?> PortfolioNames()
                => Json.TryGetProperty("portfolios", out var portfolios) && portfolios.ValueKind == JsonValueKind.Array
                    ? portfolios.EnumerateArray().Select(portfolio => portfolio.GetProperty("name").GetString()).ToList()
                    : [];
        }
    }
}
