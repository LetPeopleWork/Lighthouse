using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Tests.API.Integration.Reports;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// What "every report in one place, created and removed in bulk" adds to the usage data a consenting browser,
    /// or a client, hands in: four events that carry their name and nothing else - the Reports page shown, a bulk
    /// creation sent, a bulk removal sent, and one PDF of several reports started. Not how many reports or owners,
    /// not which, not their names, windows, outcomes or reasons. Each is appended after the PDF export, so every
    /// event named before keeps the number older browsers and instances still send.
    ///
    /// Written as what a browser posts and what reaches the collector, naming everything as text, because that is
    /// how it travels.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("epic-6350-bulk-reports")]
    public class BulkReportsUsageEventTests : UsageDataCollectorObservationTest
    {
        private const string TheLastEventNamedBeforeThem = "ReportPdfExportStarted";

        private const string UsageDataPage = "docs/settings/usagedata.md";

        private static readonly Dictionary<string, int> EveryEventNamedBeforeThem = new(StringComparer.Ordinal)
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

        // @driving_port @real-io @kpi @contract-shape:bounded-change
        [TestCase("AllReportsListed", Category = "us-01,slice-01")]
        [TestCase("ReportBulkCreationStarted", Category = "us-02a,slice-02a")]
        [TestCase("ReportBulkRemovalStarted", Category = "us-05,slice-05", Ignore = PendingEpic6350.Slice05)]
        [TestCase("ReportCombinedPdfExportStarted", Category = "us-07,slice-07", Ignore = PendingEpic6350.Slice07)]
        public async Task The_event_travels_with_its_name_and_nothing_else(string name)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(name, string.Empty));
            var answer = await handedIn.Content.ReadAsStringAsync();
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), answer);
                Assert.That(messages, Has.Count.EqualTo(1));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(name));
                Assert.That(PropertyNames(messages[0].GetProperty("properties")), Is.EquivalentTo(EverythingThePageSaysTravels),
                    "nothing about the reports, their owners, how many there were or how a run ended may travel with the event");
            }
        }

        // @driving_port @real-io @error @contract-shape:unbounded-preservation
        [TestCase("AllReportsListed", ",\"reportTemplate\":\"ThenAndNow\"", Category = "us-01,slice-01")]
        [TestCase("AllReportsListed", ",\"route\":\"TeamDetail_Reports\"", Category = "us-01,slice-01")]
        [TestCase("ReportBulkCreationStarted", ",\"reportTemplate\":\"ThenAndNow\"", Category = "us-02a,slice-02a")]
        [TestCase("ReportBulkCreationStarted", ",\"updates\":\"Now\"", Category = "us-02a,slice-02a")]
        [TestCase("ReportBulkRemovalStarted", ",\"reportTemplate\":\"ThenAndNow\"", Category = "us-05,slice-05", Ignore = PendingEpic6350.Slice05)]
        [TestCase("ReportBulkRemovalStarted", ",\"refinementVerdict\":\"In\"", Category = "us-05,slice-05", Ignore = PendingEpic6350.Slice05)]
        [TestCase("ReportCombinedPdfExportStarted", ",\"reportTemplate\":\"ThenAndNow\"", Category = "us-07,slice-07", Ignore = PendingEpic6350.Slice07)]
        [TestCase("ReportCombinedPdfExportStarted", ",\"route\":\"TeamDetail_Reports\"", Category = "us-07,slice-07", Ignore = PendingEpic6350.Slice07)]
        public async Task The_event_carrying_anything_beside_its_name_is_refused(string name, string parts)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, AnEvent(name, string.Empty));

            using var refused = await HandInAsync(token, AnEvent(name, parts));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        /// <summary>The name is the contract and the number is whatever is next when the slice lands, so this pins the order, never the number.</summary>
        // @contract-shape:bounded-change
        [TestCase("AllReportsListed", Category = "us-01,slice-01")]
        [TestCase("ReportBulkCreationStarted", Category = "us-02a,slice-02a")]
        [TestCase("ReportBulkRemovalStarted", Category = "us-05,slice-05", Ignore = PendingEpic6350.Slice05)]
        [TestCase("ReportCombinedPdfExportStarted", Category = "us-07,slice-07", Ignore = PendingEpic6350.Slice07)]
        public void The_event_is_appended_after_the_PDF_export_never_inserted(string name)
        {
            var named = TheEventsUsageDataNames();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named.Keys, Does.Contain(name), $"{name} is not on the list");
                Assert.That(
                    named.TryGetValue(name, out var itsNumber) ? itsNumber - named[TheLastEventNamedBeforeThem] : 0,
                    Is.GreaterThan(0),
                    $"{name} must come after {TheLastEventNamedBeforeThem}");
            }
        }

        // Holds today and must keep holding as each of the four is appended.
        // @contract-shape:unbounded-preservation
        [Test]
        public void Every_event_named_before_them_keeps_its_number()
        {
            var named = TheEventsUsageDataNames();
            var before = EveryEventNamedBeforeThem.Keys.Where(named.ContainsKey).ToDictionary(key => key, key => named[key], StringComparer.Ordinal);

            Assert.That(before, Is.EquivalentTo(EveryEventNamedBeforeThem), "an event named before these changed its number");
        }

        // Data leaving that the page does not describe is data nobody was told about.
        // @contract-shape:bounded-change
        [TestCase("Every report was listed", Category = "us-01,slice-01")]
        [TestCase("Reports were created for several Teams or Portfolios", Category = "us-02a,slice-02a")]
        [TestCase("Several reports were removed at once", Category = "us-05,slice-05", Ignore = PendingEpic6350.Slice05)]
        [TestCase("Several reports were exported as one PDF", Category = "us-07,slice-07", Ignore = PendingEpic6350.Slice07)]
        public void The_usage_data_page_has_one_row_for_the_event_saying_nothing_travels_with_it(string rowSays)
        {
            var rows = RowsStartingWith(rowSays);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1), "the page has one row for the event");
                Assert.That(rows, Has.All.Contains("Nothing"));
            }
        }

        // @contract-shape:bounded-change
        [TestCase("A report was created", "Create reports", Category = "us-02a,slice-02a")]
        [TestCase("A report was deleted", "already gone", Category = "us-05,slice-05", Ignore = PendingEpic6350.Slice05)]
        [TestCase("A report was exported as a PDF", "one report selected", Category = "us-07,slice-07", Ignore = PendingEpic6350.Slice07)]
        public void The_usage_data_page_says_what_the_bulk_actions_add_to_an_existing_event(string rowSays, string itNowSays)
        {
            var rows = RowsStartingWith(rowSays);

            Assert.That(rows, Has.Count.EqualTo(1).And.All.Contains(itNowSays));
        }

        // --- Steps ---

        private static string AnEvent(string name, string parts)
            => $"{{\"events\":[{{\"name\":\"{name}\"{parts},\"offsetMs\":0,\"sequence\":0}}]}}";

        private async Task ThePlainEventIsAccepted(string token, string body)
        {
            using var complete = await HandInAsync(token, body);
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
