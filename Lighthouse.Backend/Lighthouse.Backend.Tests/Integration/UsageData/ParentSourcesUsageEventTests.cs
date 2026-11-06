using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// What ordered parent sources add to the usage data a consenting browser hands in: one event that carries its
    /// name and nothing else. Not which fields, not how many entries, not the Team or Portfolio, not the tracker. It
    /// is appended after the last event named so far, so every event before it keeps the number older browsers and
    /// instances still send.
    ///
    /// Written as what a browser posts and what reaches the collector, naming everything as text, because that is
    /// how it travels.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class ParentSourcesUsageEventTests : UsageDataCollectorObservationTest
    {
        private const string ParentSourcesSaved = "ParentSourcesSavedWithSeveralEntries";

        private const string TheLastEventNamedBeforeIt = "ReportCombinedPdfExportStarted";

        private const string UsageDataPage = "docs/settings/usagedata.md";

        private const string ItsRowOnThePage = "Parent sources were saved with several entries";

        private static readonly Dictionary<string, int> EveryEventNamedBeforeIt = new(StringComparer.Ordinal)
        {
            ["TeamTabOpened"] = 0,
            ["PortfolioTabOpened"] = 1,
            ["TeamCreated"] = 2,
            ["TeamDeleted"] = 3,
            ["PortfolioCreated"] = 4,
            ["PortfolioDeleted"] = 5,
            ["TeamManualForecastRun"] = 6,
            ["WorkTrackingSystemConnected"] = 7,
            ["TeamRefreshTriggered"] = 8,
            ["PortfolioRefreshTriggered"] = 9,
            ["OptionalFeatureToggled"] = 10,
            ["TeamForecastRealityCheckRun"] = 11,
            ["TeamRefinementConfigured"] = 12,
            ["TeamSizingVoteCast"] = 13,
            ["TeamSizingReadinessReached"] = 14,
            ["TeamRefinementDayVerdictShown"] = 15,
            ["ReportCreated"] = 16,
            ["ReportOpened"] = 17,
            ["ReportDeleted"] = 18,
            ["WorkTrackingSystemFieldsChanged"] = 19,
            ["ReportPdfExportStarted"] = 20,
            ["AllReportsListed"] = 21,
            ["ReportBulkCreationStarted"] = 22,
            ["ReportBulkRemovalStarted"] = 23,
            ["ReportCombinedPdfExportStarted"] = 24,
        };

        private static readonly string[] EverythingThePageSaysTravels =
        [
            "version",
            "deployment_mode",
            "licence_tier",
            "auth_enabled",
            "source",
            "$ip",
            "$geoip_disable",
        ];

        [Test]
        public async Task The_event_travels_with_its_name_and_nothing_else()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(string.Empty));
            var answer = await handedIn.Content.ReadAsStringAsync();
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), answer);
                Assert.That(messages, Has.Count.EqualTo(1));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(ParentSourcesSaved));
                Assert.That(PropertyNames(messages[0].GetProperty("properties")), Is.EquivalentTo(EverythingThePageSaysTravels),
                    "nothing about the list, its fields, its length or its owner may travel with the event");
            }
        }

        [TestCase(",\"route\":\"TeamDetail_Settings\"")]
        [TestCase(",\"workTrackingSystem\":\"Jira\"")]
        [TestCase(",\"updates\":\"Now\"")]
        [TestCase(",\"reportTemplate\":\"ThenAndNow\"")]
        [TestCase(",\"featuresHeld\":\"UpTo20\"")]
        public async Task The_event_carrying_anything_beside_its_name_is_refused(string parts)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token);

            using var refused = await HandInAsync(token, AnEvent(parts));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        /// <summary>The name is the contract and the number is whatever is next when this lands, so this pins the order, never the number.</summary>
        [Test]
        public void The_event_is_appended_after_the_last_one_named_never_inserted()
        {
            var named = TheEventsUsageDataNames();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named.Keys, Does.Contain(ParentSourcesSaved), $"{ParentSourcesSaved} is not on the list");
                Assert.That(
                    named.TryGetValue(ParentSourcesSaved, out var itsNumber) ? itsNumber - named[TheLastEventNamedBeforeIt] : 0,
                    Is.GreaterThan(0),
                    $"{ParentSourcesSaved} must come after {TheLastEventNamedBeforeIt}");
            }
        }

        [Test]
        public void Every_event_named_before_it_keeps_its_number()
        {
            var named = TheEventsUsageDataNames();
            var before = EveryEventNamedBeforeIt.Keys.Where(named.ContainsKey).ToDictionary(key => key, key => named[key], StringComparer.Ordinal);

            Assert.That(before, Is.EquivalentTo(EveryEventNamedBeforeIt), "an event named before this one changed its number");
        }

        // Data leaving that the page does not describe is data nobody was told about.
        [Test]
        public void The_usage_data_page_has_one_row_for_the_event_saying_nothing_travels_with_it()
        {
            var rows = RowsStartingWith(ItsRowOnThePage);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1), "the page has one row for the event");
                Assert.That(rows, Has.All.Contains("Nothing. **Not which fields, not how many, not whose**"));
            }
        }

        // --- Steps ---

        private static string AnEvent(string parts)
            => $"{{\"events\":[{{\"name\":\"{ParentSourcesSaved}\"{parts},\"offsetMs\":0,\"sequence\":0}}]}}";

        private async Task ThePlainEventIsAccepted(string token)
        {
            using var complete = await HandInAsync(token, AnEvent(string.Empty));
            await EverythingTheCollectorReceived();
            Outbound.Clear();

            Assert.That(complete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                "the bare event is accepted, so the refusal that follows is about what differs");
        }

        private async Task ThenItIsRefusedAndNothingIsForwarded(HttpResponseMessage refused)
        {
            var forwarded = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(forwarded, Is.Empty);
            }
        }

        private static List<JsonElement> EveryMessageIn(string sent)
        {
            if (string.IsNullOrEmpty(sent))
            {
                return [];
            }

            using var document = JsonDocument.Parse(sent);
            return [.. document.RootElement.EnumerateArray().Select(message => message.Clone())];
        }

        private static List<string> PropertyNames(JsonElement carried)
            => [.. carried.EnumerateObject().Select(property => property.Name)];

        private static Dictionary<string, int> TheEventsUsageDataNames()
        {
            var vocabulary = typeof(Backend.Program).Assembly.GetTypes()
                .First(type => type.IsEnum && type.Name.Equals("UsageDataEventName", StringComparison.Ordinal));

            return Enum.GetNames(vocabulary).ToDictionary(name => name, name => (int)Enum.Parse(vocabulary, name), StringComparer.Ordinal);
        }

        private static List<string> RowsStartingWith(string rowSays)
            => [.. TheUsageDataPage().Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith($"| {rowSays} |", StringComparison.Ordinal))];

        private static string TheUsageDataPage()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the docs read.");
            var repositoryRoot = Directory.GetParent(directory!.FullName)!.FullName;

            return File.ReadAllText(Path.Combine(repositoryRoot, UsageDataPage.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
