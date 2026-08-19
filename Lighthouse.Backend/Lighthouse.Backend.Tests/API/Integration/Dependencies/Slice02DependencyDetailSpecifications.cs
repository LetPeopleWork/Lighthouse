using Lighthouse.Backend.API;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog.Events;
using System.Globalization;
using System.Net;
using System.Reflection;
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
        /// <summary>
        /// What a withheld entry is still allowed to say: that it is withheld, why Lighthouse will not act
        /// on it, and where the link was read from. None of the three names the Feature or says where it
        /// lives, and every other value has to be absent for the entry to disclose nothing.
        /// </summary>
        private static readonly string[] MaySurviveWithholding = ["isWithheld", "notHonouredReason", "source"];

        /// <summary>
        /// Everything a warning on a Feature row is allowed to carry: which dependency it is about, and
        /// what is wrong with it as codes. Anything else would be text the instance cannot rename.
        /// </summary>
        private static readonly string[] WhatAWarningMaySay =
        [
            "blockerReferenceId",
            "blockerName",
            "isWithheld",
            "notHonouredReason",
            "blockerPositionedBelow",
        ];

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

        /// <summary>
        /// A second Portfolio refreshed on its own, which is how a Feature ends up somewhere the first
        /// Portfolio cannot see: the reference resolves, the Feature is right there, and the two still
        /// share no Portfolio.
        /// </summary>
        private Task GivenAnotherPortfolioRefreshed(int portfolioId, TrackedFeature theFeatureItWaitsOn)
            => DriveAPortfolioRefresh(portfolioId, theFeatureItWaitsOn);

        /// <summary>
        /// A refresh handing the Features back in a given sequence, which is what puts one below another:
        /// nothing here has a place of its own, so they are numbered in the order they arrived.
        /// </summary>
        private Task GivenAPortfolioWhereTheOneItWaitsOnComesLast(int portfolioId, params TrackedFeature[] inThisOrder)
            => DriveAPortfolioRefresh(portfolioId, inThisOrder);

        private Task<Dictionary<string, int>> GivenTheOrderEveryFeatureIsIn() => ReadTheOrderEveryFeatureIsIn();

        private Task WhenARefreshRuns(int portfolioId, params TrackedFeature[] rowsFromTheTracker)
            => DriveAPortfolioRefresh(portfolioId, rowsFromTheTracker);

        /// <summary>
        /// A chain of Features each waiting on the next, with the last waiting on the first. Long enough
        /// that a walk taking one step per hop on the call stack would not come back.
        /// </summary>
        private static TrackedFeature[] AChainOfFeaturesClosingOnItself(int howMany)
            => Enumerable.Range(1, howMany)
                .Select(place => new TrackedFeature(
                    ChainedFeature(place),
                    $"Link {place} of the chain",
                    [ChainedFeature(place == howMany ? 1 : place + 1)]))
                .ToArray();

        private static string ChainedFeature(int place) => $"CHAIN-{place}";

        private List<string> GivenEverythingRecordedAboutTheDependencies() => ReadEverythingRecordedAboutTheDependencies();

        private void GivenTheTeamBehindItHasNoMeasuredDelivery(string featureReferenceId)
            => GiveItWorkNobodyHasMeasured(featureReferenceId, remainingWorkItems: 3);

        private void GivenTheWorkOnItIsAllFinished(string featureReferenceId)
            => GiveItWorkNobodyHasMeasured(featureReferenceId, remainingWorkItems: 0);

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

        private async Task<List<JsonElement>> WhenAReaderOfOnlyOnePortfolioOpensWhatItWaitsOn(
            string featureReferenceId, int theOnlyPortfolioTheyCanRead)
        {
            using var reader = Factory.CreateClient().AsPortfolioViewer(theOnlyPortfolioTheyCanRead);

            var (status, entries) = await AskFor(reader, "latest", featureReferenceId);

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK),
                $"A reader of the Portfolio {featureReferenceId} is in must be handed what it waits on.");

            return entries;
        }

        private async Task<HttpStatusCode> WhenAReaderOfAnotherPortfolioOpensWhatItWaitsOn(
            string featureReferenceId, int theOnlyPortfolioTheyCanRead)
        {
            var featureId = TheFeatureIdOf(featureReferenceId);

            using var reader = Factory.CreateClient().AsPortfolioViewer(theOnlyPortfolioTheyCanRead);
            using var response = await reader.GetAsync($"/api/latest/features/{featureId}/dependencies");

            return response.StatusCode;
        }

        /// <summary>
        /// The Features view, read the way it reads: one request for the whole list. A warning that needed
        /// a second request would be a warning the reader has to go and ask for, which is the opposite of
        /// finding broken links by scanning.
        /// </summary>
        private async Task<Dictionary<string, JsonElement>> WhenTheDeliveryLeadOpensTheFeaturesView()
        {
            using var response = await Client.GetAsync("/api/latest/features");
            response.EnsureSuccessStatusCode();

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return payload.RootElement.EnumerateArray()
                .ToDictionary(row => row.GetProperty("referenceId").GetString()!, row => row.Clone());
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

        private static void ThenNoEntryClaimsASourceOutsideTheWorkTrackingSystem(List<JsonElement> entries)
        {
            var claimed = entries.Select(entry => entry.GetProperty("source").GetString()).Distinct().ToList();

            Assert.That(claimed, Has.All.EqualTo(nameof(DependencySource.TrackerLink)),
                $"Lighthouse records no dependency of its own, so nothing here can have come from anywhere else. Claimed: {string.Join(", ", claimed)}");
        }

        private static void ThenTheReasonsAreExactly(NotHonouredReason[] expectedReasons)
        {
            Assert.That(Enum.GetValues<NotHonouredReason>(), Is.EquivalentTo(expectedReasons),
                "A reader meeting a reason nobody has heard of has to guess, so widening this set has to be somebody's decision rather than a side effect.");
        }

        private static void ThenOneEntryIsWithheld(List<JsonElement> entries)
        {
            Assert.That(entries.Count(IsWithheld), Is.EqualTo(1),
                $"A Feature the reader may not see is still a Feature being waited on, and has to be shown as one. Listed: {Describe(entries)}");
        }

        /// <summary>
        /// The whole of a withheld entry. Asserted over every value it carries rather than over the three
        /// somebody thought of, because a field added next year discloses just as much as these do.
        /// </summary>
        private static void ThenTheWithheldEntryDisclosesNothingButThatItExists(List<JsonElement> entries)
        {
            var withheld = entries.Single(IsWithheld);
            var disclosed = withheld.EnumerateObject()
                .Where(property => !MaySurviveWithholding.Contains(property.Name))
                .Where(property => property.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
                .Where(property => property.Value.ToString() is not ("" or "0" or "[]" or nameof(StateCategories.Unknown)))
                .Select(property => property.Name)
                .ToList();

            Assert.That(disclosed, Is.Empty,
                $"A withheld entry says that something is being waited on and nothing else about it. Disclosed: {string.Join(", ", disclosed)} in {withheld}");
        }

        private static void ThenTheListIsAsLongAsWhatItWaitsOn(List<JsonElement> entries, int expectedEntries)
        {
            Assert.That(entries, Has.Count.EqualTo(expectedEntries),
                $"Every Feature waited on gets an entry, readable or not, or the list stops accounting for the number above it. Listed: {Describe(entries)}");
        }

        private static void ThenBothReadersWereShownTheSame(List<JsonElement> onePerson, List<JsonElement> another)
        {
            Assert.That(Describe(another), Is.EqualTo(Describe(onePerson)),
                "Being unable to change a Portfolio is no reason to be told less about what its Features are waiting on.");
        }

        /// <summary>
        /// Asked of the API surface rather than of one route: a route that added, removed or suppressed a
        /// dependency would answer "may this reader change one?" simply by existing, whatever it then went
        /// on to check.
        /// </summary>
        private static void ThenNothingInTheApiWritesADependency()
        {
            var writingRoutes = typeof(FeaturesController).Assembly.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
                .SelectMany(controller => controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(ChangesSomething)
                .Where(IsAboutADependency)
                .Select(action => $"{action.DeclaringType?.Name}.{action.Name}")
                .ToList();

            Assert.That(writingRoutes, Is.Empty,
                $"Lighthouse records no dependency of its own, so there is nothing for a write route to be for. Found: {string.Join(", ", writingRoutes)}");
        }

        /// <summary>
        /// The whole rendered order, compared as a whole. Reading only the two Features under suspicion
        /// would miss a read that quietly renumbered everything else around them.
        /// </summary>
        private static void ThenTheOrderEveryFeatureIsInIsUnchanged(
            Dictionary<string, int> before, Dictionary<string, JsonElement> rows)
        {
            var after = rows.ToDictionary(row => row.Key, row => row.Value.GetProperty("position").GetInt32());
            var moved = before.Keys.Union(after.Keys)
                .Where(feature => PlaceOf(before, feature) != PlaceOf(after, feature))
                .Select(feature => $"{feature}: was {PlaceOf(before, feature)}, now {PlaceOf(after, feature)}")
                .ToList();

            Assert.That(moved, Is.Empty,
                $"Saying the order looks odd is not permission to change it. Moved: {string.Join(" | ", moved)}");
        }

        private static string PlaceOf(Dictionary<string, int> order, string featureReferenceId)
            => order.TryGetValue(featureReferenceId, out var place) ? place.ToString() : "nowhere";

        private static FeatureRow ThenTheRowFor(Dictionary<string, JsonElement> rows, string featureReferenceId)
        {
            Assert.That(rows.ContainsKey(featureReferenceId), Is.True,
                $"The Features view must carry {featureReferenceId} for anything to be said about its row.");

            return new FeatureRow(rows[featureReferenceId], featureReferenceId);
        }

        /// <summary>
        /// Every word a reader sees is built in their own instance's vocabulary, so a warning may carry a
        /// code and a name and nothing else. A sentence in the payload is a sentence nobody can rename.
        /// </summary>
        private static void ThenNoWarningCarriesASentenceNobodyCanRename(Dictionary<string, JsonElement> rows)
        {
            var unexpected = rows.Values
                .SelectMany(WarningsOn)
                .SelectMany(warning => warning.EnumerateObject())
                .Select(property => property.Name)
                .Distinct()
                .Where(name => !WhatAWarningMaySay.Contains(name))
                .ToList();

            Assert.That(unexpected, Is.Empty,
                $"A warning says which dependency and why, in codes the client renders. Carried as well: {string.Join(", ", unexpected)}");
        }

        private static List<JsonElement> WarningsOn(JsonElement row)
            => row.TryGetProperty("dependencyWarnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array
                ? warnings.EnumerateArray().ToList()
                : [];

        private static bool ChangesSomething(MethodInfo action)
            => action.GetCustomAttributes().Any(attribute =>
                attribute is HttpPostAttribute or HttpPutAttribute or HttpDeleteAttribute or HttpPatchAttribute);

        private static bool IsAboutADependency(MethodInfo action)
            => Mentions(action.Name)
                || Mentions(action.ReturnType.ToString())
                || action.GetParameters().Any(parameter => Mentions(parameter.ParameterType.ToString()));

        private static bool Mentions(string name)
            => name.Contains("Dependenc", StringComparison.OrdinalIgnoreCase);

        private static bool IsWithheld(JsonElement entry)
            => entry.TryGetProperty("isWithheld", out var withheld) && withheld.ValueKind == JsonValueKind.True;

        private static string Describe(List<JsonElement> entries)
            => string.Join(" | ", entries.Select(entry => entry.ToString()));

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

        private static void ThenEveryFeatureInTheChainWarnsAboutTheLoop(Dictionary<string, JsonElement> rows, int howMany)
        {
            var silent = Enumerable.Range(1, howMany)
                .Select(ChainedFeature)
                .Where(feature => !rows.TryGetValue(feature, out var row) || !SaysItIsInALoop(row))
                .ToList();

            Assert.That(silent, Is.Empty,
                $"Every Feature going round the circle is waiting on every other one, so none of them can be left out of it. Silent: {string.Join(", ", silent)}");
        }

        private static bool SaysItIsInALoop(JsonElement row)
            => WarningsOn(row).Any(warning =>
                warning.GetProperty("notHonouredReason").GetString() == nameof(NotHonouredReason.InALoop));

        private void ThenNothingAboutTheDependenciesWasRecorded(List<string> before)
        {
            var after = ReadEverythingRecordedAboutTheDependencies();

            Assert.That(after, Is.EqualTo(before),
                "Working out what is wrong with a dependency is reading, and a verdict written down would be a second place the answer lives.");
        }

        private void ThenTheOperatorWasWarnedOnceAboutACircleNaming(params string[] members)
        {
            var aboutCircles = LinesAboutDependencies(LogEventLevel.Warning);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aboutCircles, Has.Count.EqualTo(1),
                    $"A circle is rare and genuinely wrong, so it is worth one line and no more. Logged: {string.Join(" | ", aboutCircles)}");
                Assert.That(members.Where(member => !aboutCircles[0].Contains(member, StringComparison.Ordinal)), Is.Empty,
                    $"An operator reading this has to be able to go and look at the Features it is about. Logged: {aboutCircles[0]}");
            }
        }

        private void ThenTheOperatorWasToldOnceHowManyCannotBeForecast(int expectedCount)
        {
            var aboutForecasting = LinesAboutDependencies(LogEventLevel.Information)
                .Where(line => line.Contains("forecast", StringComparison.OrdinalIgnoreCase))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aboutForecasting, Has.Count.EqualTo(1),
                    $"One line for the lot of them: a line each would bury the summary an operator actually reads. Logged: {string.Join(" | ", aboutForecasting)}");
                Assert.That(aboutForecasting[0], Does.Contain(expectedCount.ToString(CultureInfo.InvariantCulture)),
                    $"A report worth reading says how many. Logged: {aboutForecasting[0]}");
            }
        }

        private void ThenNothingWasSaidAboutDependencies()
        {
            var aboutDependencies = LinesAboutDependencies(LogEventLevel.Verbose);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CapturedLogs.SawAnything, Is.True,
                    "A capture that quietly stopped working would make the assertion below unable to fail.");
                Assert.That(aboutDependencies, Is.Empty,
                    $"Dependencies that are all fine are not news, and a line saying so every refresh is noise. Logged: {string.Join(" | ", aboutDependencies)}");
            }
        }

        /// <summary>
        /// Every line the refresh wrote about what Features wait on, at or above the level asked for. Read
        /// by what the line is about rather than by which class wrote it, because an operator finds it the
        /// same way.
        /// </summary>
        private List<string> LinesAboutDependencies(LogEventLevel level)
            => CapturedLogs.AtOrAbove(level)
                .Where(line => line.Contains("waiting on", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("depends on", StringComparison.OrdinalIgnoreCase))
                .ToList();

        // --- Reading the store ---

        /// <summary>
        /// Every stored dependency as one comparable line, so a read that quietly recorded what it worked
        /// out shows up as a difference rather than as nothing at all.
        /// </summary>
        private List<string> ReadEverythingRecordedAboutTheDependencies()
            => ReadStoredDependencies()
                .Select(stored => $"{stored.FeatureReferenceId}|{stored.WaitsOnReferenceId}|{stored.Source}|{stored.KeyedToFeatureId}")
                .Order()
                .ToList();

        /// <summary>
        /// Where every Feature sits, read from the store rather than from the payload under test, so the
        /// comparison afterwards is against something the read being judged had no hand in.
        /// </summary>
        private async Task<Dictionary<string, int>> ReadTheOrderEveryFeatureIsIn()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var places = await scope.ServiceProvider.GetRequiredService<IFeaturePositionMap>().GetAsync();

            var namedById = context.Features.AsNoTracking().ToDictionary(feature => feature.Id, feature => feature.ReferenceId);

            return places
                .Where(place => namedById.ContainsKey(place.Key))
                .ToDictionary(place => namedById[place.Key], place => place.Value);
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
        /// One Feature's row on the Features view, kept beside the id it was found by so every failure
        /// says whose row disappointed.
        /// </summary>
        private readonly record struct FeatureRow(JsonElement Json, string ReferenceId)
        {
            public void WarnsAbout(string blockerReferenceId, string blockerName, string expectedReason)
            {
                var warning = WarningAbout(blockerReferenceId);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(warning?.GetProperty("blockerName").GetString(), Is.EqualTo(blockerName),
                        $"A warning that does not name what {ReferenceId} is waiting on leaves the reader to go and find it. Row: {Json}");
                    Assert.That(warning?.GetProperty("notHonouredReason").GetString(), Is.EqualTo(expectedReason),
                        $"A warning has to say which of the things that can be wrong this one is. Row: {Json}");
                }
            }

            public void WarnsThatWhatItWaitsOnSitsBelowIt(string blockerReferenceId)
            {
                var warning = WarningAbout(blockerReferenceId);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(warning?.GetProperty("blockerPositionedBelow").GetBoolean(), Is.True,
                        $"An arrangement that reads oddly is worth saying out loud on {ReferenceId}'s row. Row: {Json}");
                    Assert.That(warning?.GetProperty("notHonouredReason").ValueKind, Is.EqualTo(JsonValueKind.Null),
                        $"The order stays the reader's, so this is a different thing to say than a dependency Lighthouse cannot act on. Row: {Json}");
                }
            }

            public void CarriesNoDependencyWarningAtAll()
                => Assert.That(WarningsOn(Json), Is.Empty,
                    $"Having a dependency is not by itself a warning, so a sound one has nothing to report. Row: {Json}");

            private JsonElement? WarningAbout(string blockerReferenceId)
            {
                var warnings = WarningsOn(Json)
                    .Where(warning => warning.GetProperty("blockerReferenceId").GetString() == blockerReferenceId)
                    .ToList();

                Assert.That(warnings, Has.Count.EqualTo(1),
                    $"{ReferenceId} must carry exactly one warning about {blockerReferenceId}. Row: {Json}");

                return warnings.SingleOrDefault();
            }
        }

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

            public void SaysItCameFromTheTrackersOwnLink()
                => Assert.That(TextOf("source"), Is.EqualTo(nameof(DependencySource.TrackerLink)),
                    $"Where a dependency was read from is what tells a reader where to go and change it. Entry: {Json}");

            public void CannotBeActedOnBecause(string expectedReason)
                => Assert.That(TextOf("notHonouredReason"), Is.EqualTo(expectedReason),
                    $"An entry Lighthouse will not act on has to say so, and say which of the reasons it is. Entry: {Json}");

            public void CarriesNoReasonAtAll()
                => Assert.That(TextOf("notHonouredReason"), Is.Null,
                    $"A dependency with nothing wrong with it carries no reason, rather than a further code meaning fine. Entry: {Json}");

            private string? TextOf(string property)
                => Json.TryGetProperty(property, out var value) ? value.GetString() : null;

            private List<string?> PortfolioNames()
                => Json.TryGetProperty("portfolios", out var portfolios) && portfolios.ValueKind == JsonValueKind.Array
                    ? portfolios.EnumerateArray().Select(portfolio => portfolio.GetProperty("name").GetString()).ToList()
                    : [];
        }
    }
}
