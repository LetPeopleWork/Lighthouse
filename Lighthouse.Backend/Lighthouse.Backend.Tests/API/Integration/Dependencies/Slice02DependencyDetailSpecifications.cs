using Lighthouse.Backend.API;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog.Events;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Step definitions for the second dependency slice. Backend-observable contract: every Feature row
    /// names the Features it waits on that this instance holds, each with a link back to the work tracking
    /// system, and says what stands against any of them - read over the real route the list is built from.
    /// </summary>
    public partial class Slice02DependencyDetailTest : DependenciesAcceptanceTest
    {
        /// <summary>
        /// What a withheld entry is still allowed to say: that it is withheld, why Lighthouse will not act
        /// on it, whether it sits below, and where the link was read from. None of the four names the
        /// Feature or says where it lives.
        /// </summary>
        private static readonly string[] MaySurviveWithholding =
            ["isWithheld", "notHonouredReason", "blockerPositionedBelow", "source"];

        /// <summary>
        /// Every field an entry carries, withheld or not. Named here so that adding one to the payload
        /// fails this test until somebody says whether a withheld entry may carry it too.
        /// </summary>
        private static readonly string[] EverythingAnEntryCarries =
        [
            "referenceId",
            "name",
            "url",
            "source",
            "notHonouredReason",
            "blockerPositionedBelow",
            "isWithheld",
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

        /// <summary>
        /// A chain of Features each waiting on the next, ending at one that waits on nothing. Every Feature
        /// but the last carries a dependency, so a read that costs something per dependency has nowhere to
        /// hide.
        /// </summary>
        private static TrackedFeature[] AChainOfFeaturesWaitingOnEachOther(int howMany)
            => Enumerable.Range(1, howMany)
                .Select(place => new TrackedFeature(
                    ChainedFeature(place),
                    $"Link {place} of the chain",
                    place == howMany ? [] : [ChainedFeature(place + 1)]))
                .ToArray();

        private List<string> GivenEverythingRecordedAboutTheDependencies() => ReadEverythingRecordedAboutTheDependencies();

        private void GivenTheTeamBehindItHasNoMeasuredDelivery(string featureReferenceId)
            => GiveItWorkNobodyHasMeasured(featureReferenceId, remainingWorkItems: 3);

        private void GivenTheWorkOnItIsAllFinished(string featureReferenceId)
            => GiveItWorkNobodyHasMeasured(featureReferenceId, remainingWorkItems: 0);

        private void GivenNoPremiumLicence()
            => LicenseServiceMock.Setup(licences => licences.CanUsePremiumFeatures()).Returns(false);

        // --- When ---

        /// <summary>
        /// The Features view, read the way it reads: one request for the whole list.
        /// </summary>
        private Task<Dictionary<string, JsonElement>> WhenTheDeliveryLeadOpensTheFeaturesView()
            => ReadTheFeaturesView(Client);

        /// <summary>
        /// The route a Portfolio's page and a Team's page read: the Features they hold, asked for by id.
        /// </summary>
        private async Task<Dictionary<string, JsonElement>> WhenOnlySomeOfTheFeaturesAreAskedFor(
            params string[] featureReferenceIds)
        {
            var ids = featureReferenceIds.Select(TheFeatureIdOf);
            var query = string.Join("&", ids.Select(id => $"featureIds={id}"));

            using var response = await Client.GetAsync($"/api/latest/features/ids?{query}");
            response.EnsureSuccessStatusCode();

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return payload.RootElement.EnumerateArray().ToDictionary(
                feature => feature.GetProperty("referenceId").GetString() ?? string.Empty,
                feature => feature.Clone());
        }

        private async Task<Dictionary<string, JsonElement>> WhenAReaderOfOnlyOnePortfolioOpensTheFeaturesView(
            int theOnlyPortfolioTheyCanRead)
        {
            using var reader = Factory.CreateClient().AsPortfolioViewer(theOnlyPortfolioTheyCanRead);

            return await ReadTheFeaturesView(reader);
        }

        /// <summary>
        /// One read of the Features view, with everything it asked the store counted. The elapsed time is
        /// written out beside the count for whoever is recording the measurement; only the count is judged.
        /// </summary>
        private async Task<int> WhenTheFeaturesViewIsReadCountingWhatItAsksTheStore()
        {
            CapturedLogs.Clear();
            var started = Stopwatch.GetTimestamp();

            using var response = await Client.GetAsync("/api/latest/features");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            var commands = CapturedLogs.AtOrAbove(LogEventLevel.Information)
                .Count(line => line.Contains("Executed DbCommand", StringComparison.Ordinal));

            TestContext.Out.WriteLine(
                $"Features view: {commands} commands, {Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0} ms, {body.Length} bytes");

            return commands;
        }

        // --- Then ---

        private static FeatureRow ThenTheRowFor(Dictionary<string, JsonElement> rows, string featureReferenceId)
        {
            Assert.That(rows.ContainsKey(featureReferenceId), Is.True,
                $"The Features view must carry {featureReferenceId} for anything to be said about its row.");

            return new FeatureRow(rows[featureReferenceId], featureReferenceId);
        }

        private static void ThenTheReasonsAreExactly(NotHonouredReason[] expectedReasons)
        {
            Assert.That(Enum.GetValues<NotHonouredReason>(), Is.EquivalentTo(expectedReasons),
                "A reader meeting a reason nobody has heard of has to guess, so widening this set has to be somebody's decision rather than a side effect.");
        }

        private static void ThenExactlyOneEntryIsWithheld(Dictionary<string, JsonElement> rows, string featureReferenceId)
        {
            var withheld = DependsOnOf(rows[featureReferenceId]).Count(IsWithheld);

            Assert.That(withheld, Is.EqualTo(1),
                $"A Feature the reader may not see is still a Feature being waited on, and has to be shown as one. Row: {rows[featureReferenceId]}");
        }

        /// <summary>
        /// The whole of a withheld entry, judged twice. First that it carries exactly the fields an entry
        /// is known to have - a field added later fails here, which is the point: whether it may survive
        /// withholding is a decision somebody has to take rather than one an empty default takes for them.
        /// Then that everything beyond the four it may say is empty.
        /// </summary>
        private static void ThenTheWithheldEntryDisclosesNothingButThatItExists(
            Dictionary<string, JsonElement> rows, string featureReferenceId)
        {
            var withheld = DependsOnOf(rows[featureReferenceId]).Single(IsWithheld);
            var carried = withheld.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            var disclosed = withheld.EnumerateObject()
                .Where(property => !MaySurviveWithholding.Contains(property.Name))
                .Where(property => property.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
                .Where(property => property.Value.ToString() is not ("" or "0" or "[]"))
                .Select(property => property.Name)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(carried, Is.EquivalentTo(EverythingAnEntryCarries),
                    $"An entry grew or lost a field. Whether the new one may survive withholding is a decision, not something an empty default settles quietly. Carried: {string.Join(", ", carried.Order(StringComparer.Ordinal))}");
                Assert.That(disclosed, Is.Empty,
                    $"A withheld entry says that something is being waited on and nothing else about it. Disclosed: {string.Join(", ", disclosed)} in {withheld}");
            }
        }

        private static void ThenBothReadersWereShownTheSame(
            Dictionary<string, JsonElement> onePerson,
            Dictionary<string, JsonElement> another,
            string featureReferenceId)
        {
            Assert.That(
                DependsOnOf(another[featureReferenceId]).Select(entry => entry.ToString()).ToList(),
                Is.EqualTo(DependsOnOf(onePerson[featureReferenceId]).Select(entry => entry.ToString()).ToList()),
                "Being unable to change a Portfolio is no reason to be told less about what its Features are waiting on.");
        }

        /// <summary>
        /// Every word a reader sees is built in their own instance's vocabulary, so an entry may carry a
        /// code and a name and nothing else. A sentence in the payload is a sentence nobody can rename.
        /// </summary>
        private static void ThenNoEntryCarriesASentenceNobodyCanRename(Dictionary<string, JsonElement> rows)
        {
            var unexpected = rows.Values
                .SelectMany(DependsOnOf)
                .SelectMany(entry => entry.EnumerateObject())
                .Select(property => property.Name)
                .Distinct()
                .Where(name => !EverythingAnEntryCarries.Contains(name))
                .ToList();

            Assert.That(unexpected, Is.Empty,
                $"An entry says which dependency and why, in codes the client renders. Carried as well: {string.Join(", ", unexpected)}");
        }

        private static void ThenEveryFeatureInTheChainSaysItIsInACircle(Dictionary<string, JsonElement> rows, int howMany)
        {
            var silent = Enumerable.Range(1, howMany)
                .Select(ChainedFeature)
                .Where(feature => !rows.TryGetValue(feature, out var row) || !SaysItIsInALoop(row))
                .ToList();

            Assert.That(silent, Is.Empty,
                $"Every Feature going round the circle is waiting on every other one, so none of them can be left out of it. Silent: {string.Join(", ", silent)}");
        }

        private static bool SaysItIsInALoop(JsonElement row)
            => DependsOnOf(row).Any(entry =>
                entry.GetProperty("notHonouredReason").GetString() == nameof(NotHonouredReason.InALoop));

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
            => order.TryGetValue(featureReferenceId, out var place) ? place.ToString(CultureInfo.InvariantCulture) : "nowhere";

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

        private static bool ChangesSomething(MethodInfo action)
            => action.GetCustomAttributes().Any(attribute =>
                attribute is HttpPostAttribute or HttpPutAttribute or HttpDeleteAttribute or HttpPatchAttribute);

        private static bool IsAboutADependency(MethodInfo action)
            => Mentions(action.Name)
                || Mentions(action.ReturnType.ToString())
                || action.GetParameters().Any(parameter => Mentions(parameter.ParameterType.ToString()));

        private static bool Mentions(string name)
            => name.Contains("Dependenc", StringComparison.OrdinalIgnoreCase);

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

        private void ThenNothingAboutTheDependenciesWasRecorded(List<string> before)
        {
            var after = ReadEverythingRecordedAboutTheDependencies();

            Assert.That(after, Is.EqualTo(before),
                "Working out what is wrong with a dependency is reading, and a verdict written down would be a second place the answer lives.");
        }

        private static void ThenTheReadAskedTheStoreTheSameNumberOfTimes(int overASmallList, int overALongOne)
        {
            Assert.That(overALongOne, Is.EqualTo(overASmallList),
                $"Working out what is wrong with a dependency has to cost what the page already paid. A count " +
                $"that grows with the list is a query per Feature, which is fine on any fixture small enough to " +
                $"write by hand. Small list: {overASmallList}, long list: {overALongOne}.");
        }

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

        private static async Task<Dictionary<string, JsonElement>> ReadTheFeaturesView(HttpClient client)
        {
            using var response = await client.GetAsync("/api/latest/features");
            response.EnsureSuccessStatusCode();

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return payload.RootElement.EnumerateArray()
                .ToDictionary(row => row.GetProperty("referenceId").GetString()!, row => row.Clone());
        }

        /// <summary>
        /// Every entry on one row, flattened to text so two reads can be compared whole rather than field
        /// by field: a comparison that looked only at the fields somebody thought of would pass while the
        /// two screens disagreed about one nobody did.
        /// </summary>
        private static List<string> TheEntriesAsRead(Dictionary<string, JsonElement> rows, string featureReferenceId)
            => [.. DependsOnOf(rows[featureReferenceId]).Select(entry => entry.ToString()).Order(StringComparer.Ordinal)];

        private static List<JsonElement> DependsOnOf(JsonElement row)
            => row.TryGetProperty("dependsOn", out var dependsOn) && dependsOn.ValueKind == JsonValueKind.Array
                ? dependsOn.EnumerateArray().ToList()
                : [];

        private static bool IsWithheld(JsonElement entry)
            => entry.TryGetProperty("isWithheld", out var withheld) && withheld.ValueKind == JsonValueKind.True;

        /// <summary>
        /// One Feature's row on the Features view, kept beside the id it was found by so every failure
        /// says whose row disappointed.
        /// </summary>
        private readonly record struct FeatureRow(JsonElement Json, string ReferenceId)
        {
            public void WaitsOn(params string[] expectedReferenceIds)
            {
                var named = DependsOnOf(Json)
                    .Select(entry => entry.GetProperty("referenceId").GetString())
                    .Order()
                    .ToArray();

                Assert.That(named, Is.EqualTo(expectedReferenceIds.Order().ToArray()),
                    $"{ReferenceId} must name exactly the Features it waits on that Lighthouse holds. Row: {Json}");
            }

            public void WaitsOnThisMany(int expectedEntries)
                => Assert.That(DependsOnOf(Json), Has.Count.EqualTo(expectedEntries),
                    $"Every Feature waited on gets an entry, readable or not, or the list quietly comes back short. Row: {Json}");

            public DependencyEntry Entry(string blockerReferenceId)
            {
                var entries = DependsOnOf(Json)
                    .Where(entry => entry.GetProperty("referenceId").GetString() == blockerReferenceId)
                    .ToList();

                Assert.That(entries, Has.Count.EqualTo(1),
                    $"{ReferenceId} must carry exactly one entry for {blockerReferenceId}. Row: {Json}");

                return new DependencyEntry(entries[0], blockerReferenceId);
            }
        }

        /// <summary>
        /// One entry on a row, kept beside the id it was found by so every failure says which entry
        /// disappointed.
        /// </summary>
        private readonly record struct DependencyEntry(JsonElement Json, string ReferenceId)
        {
            public void Names(string expectedName)
                => Assert.That(TextOf("name"), Is.EqualTo(expectedName),
                    $"An entry has to name the Feature being waited on, or the reader is looking at an id. Entry: {Json}");

            public void LeadsTo(string expectedUrl)
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

            public void SitsBelowTheFeatureWaitingOnIt()
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(Json.GetProperty("blockerPositionedBelow").GetBoolean(), Is.True,
                        $"An arrangement that reads oddly is worth saying out loud. Entry: {Json}");
                    Assert.That(Json.GetProperty("notHonouredReason").ValueKind, Is.EqualTo(JsonValueKind.Null),
                        $"The order stays the reader's, so this is a different thing to say than a dependency Lighthouse cannot act on. Entry: {Json}");
                }
            }

            public void HasNothingWrongWithIt()
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(Json.GetProperty("notHonouredReason").ValueKind, Is.EqualTo(JsonValueKind.Null),
                        $"Having a dependency is not by itself a problem. Entry: {Json}");
                    Assert.That(Json.GetProperty("blockerPositionedBelow").GetBoolean(), Is.False,
                        $"Nothing wrong with it means nothing at all to say, the order included. Entry: {Json}");
                }
            }

            private string? TextOf(string property)
                => Json.TryGetProperty(property, out var value) ? value.GetString() : null;
        }
    }
}
