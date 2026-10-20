using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// What exporting a report as a PDF adds to the usage data a consenting browser hands in. Choosing Export
    /// PDF travels as one event carrying which type of report it was, from the same closed list a report being
    /// created or opened names, and nothing else - not the report, its owner, its dates, its numbers, nor
    /// whether a file was saved. The page drawn for printing is not a page anyone opened, so its address is
    /// never published.
    ///
    /// Written as what a browser posts and what reaches the collector, naming everything as text, because
    /// that is how it travels. Whether a browser that did not agree sends anything is the pipe's promise and
    /// is pinned once for every event by the fixtures beside this one.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("epic-6298-report-pdf-export")]
    [Category("slice-01")]
    public class ReportPdfExportUsageEventTests : UsageDataCollectorObservationTest
    {
        private const string PdfExportStarted = "ReportPdfExportStarted";

        private const string ThenAndNow = "ThenAndNow";

        private const string TemplateOnTheWire = "report_template";

        private const string TheLastEventNamedBeforeIt = "WorkTrackingSystemFieldsChanged";

        private const string UsageDataPage = "docs/settings/usagedata.md";

        /// <summary>
        /// Every event named before this one, with the number it travels as. A browser or an instance on an
        /// older release still sends these numbers, so none of them may move.
        /// </summary>
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

        private static readonly string[] EverythingThePageSaysTravelsWithATemplate = [.. EverythingThePageSaysTravels, TemplateOnTheWire];

        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("real-io")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        public async Task A_PDF_export_travels_with_its_report_type_and_nothing_else()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(PdfExportStarted, TheType(ThenAndNow)));
            var answer = await handedIn.Content.ReadAsStringAsync();
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), answer);
                Assert.That(messages, Has.Count.EqualTo(1));
            }

            var properties = messages[0].GetProperty("properties");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(PdfExportStarted));
                Assert.That(PropertyNames(properties), Is.EquivalentTo(EverythingThePageSaysTravelsWithATemplate),
                    "nothing about the report, its owner, its windows, its numbers or the file may travel with the event");
                Assert.That(properties.GetProperty(TemplateOnTheWire).GetString(), Is.EqualTo(ThenAndNow));
            }
        }

        // The type is the whole of what the event says: without one from the list, or with anything beside it, it is refused.
        [TestCase("")]
        [TestCase(",\"reportTemplate\":\"Signal\"")]
        [TestCase(",\"reportTemplate\":\"then-and-now\"")]
        [TestCase(",\"reportTemplate\":\"ThenAndNow\",\"route\":\"TeamDetail_Reports\"")]
        [TestCase(",\"reportTemplate\":\"ThenAndNow\",\"refinementVerdict\":\"In\"")]
        [TestCase(",\"reportTemplate\":\"ThenAndNow\",\"updates\":\"Now\"")]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("real-io")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_PDF_export_without_a_type_from_the_list_or_carrying_anything_more_is_refused(string parts)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, AnEvent(PdfExportStarted, TheType(ThenAndNow)));

            using var refused = await HandInAsync(token, AnEvent(PdfExportStarted, parts));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        // Holds today and must keep holding: exporting adds one event to those that name a type, and no other.
        [TestCase("ReportDeleted")]
        [TestCase("TeamCreated")]
        [TestCase("WorkTrackingSystemFieldsChanged")]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("real-io")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Any_other_event_that_names_no_type_still_refuses_one(string name)
        {
            var token = await ABrowserThatAgreedAsync();
            var whatItCarries = WhatTheEventCarriesOfItsOwn(name);
            await ThePlainEventIsAccepted(token, AnEvent(name, whatItCarries));

            using var refused = await HandInAsync(token, AnEvent(name, whatItCarries + TheType(ThenAndNow)));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        /// <summary>
        /// The name is the contract and the number is whatever is next when the slice lands, so this pins the
        /// order the name was appended in rather than its number.
        /// </summary>
        [Test]
        [Category("us-01")]
        [Category("contract-shape:bounded-change")]
        public void The_export_is_appended_to_the_list_of_names_never_inserted()
        {
            var named = TheEventsUsageDataNames();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named.Keys, Does.Contain(PdfExportStarted), $"{PdfExportStarted} is not on the list");
                Assert.That(
                    named.TryGetValue(PdfExportStarted, out var itsNumber) ? itsNumber - named[TheLastEventNamedBeforeIt] : 0,
                    Is.GreaterThan(0),
                    $"{PdfExportStarted} must come after {TheLastEventNamedBeforeIt}");
            }
        }

        // Holds today and must keep holding once the export is named.
        [Test]
        [Category("us-01")]
        [Category("contract-shape:unbounded-preservation")]
        public void Every_event_named_before_the_export_keeps_its_number()
        {
            var named = TheEventsUsageDataNames();

            Assert.That(TheNumbersOf(named, EveryEventNamedBeforeIt.Keys), Is.EquivalentTo(EveryEventNamedBeforeIt),
                "an event named before the export changed its number");
        }

        // Data leaving that the page does not describe is data nobody was told about.
        [Test]
        [Category("us-01")]
        [Category("contract-shape:bounded-change")]
        public void The_usage_data_page_has_a_row_for_a_report_exported_as_a_PDF_naming_its_type()
        {
            var rows = RowsStartingWith("A report was exported as a PDF");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1), "the page has one row for the event");
                Assert.That(rows, Has.All.Contains($"`{ThenAndNow}`"));
            }
        }

        [Test]
        [Category("us-01")]
        [Category("contract-shape:bounded-change")]
        public void The_usage_data_page_says_an_export_also_names_which_type_of_report()
        {
            var rows = RowsStartingWith("Which type of report");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1), "the page has one row for the field");
                Assert.That(rows, Has.All.Contains("exported"));
            }
        }

        // Holds today and must keep holding: the print page is no page anyone opened, so its address is never published.
        [Test]
        [Category("us-01")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public void The_usage_data_page_publishes_no_print_address()
        {
            var page = TheUsageDataPage();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(page, Does.Contain("`/teams/:id/reports`"));
                Assert.That(page, Does.Not.Contain("/print/"));
            }
        }

        // --- Steps ---

        private static string TheType(string type) => $",\"reportTemplate\":\"{type}\"";

        private static string AnEvent(string name, string parts)
            => $"{{\"events\":[{{\"name\":\"{name}\"{parts},\"offsetMs\":0,\"sequence\":0}}]}}";

        private static string WhatTheEventCarriesOfItsOwn(string name)
            => name == "WorkTrackingSystemFieldsChanged" ? ",\"updates\":\"Now\"" : string.Empty;

        private async Task ThePlainEventIsAccepted(string token, string body)
        {
            using var complete = await HandInAsync(token, body);
            await EverythingTheCollectorReceived();
            Outbound.Clear();

            Assert.That(complete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                "the complete event is accepted, so the refusal that follows is about what differs");
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

        private static Dictionary<string, int> TheNumbersOf(Dictionary<string, int> named, IEnumerable<string> names)
            => names.Where(named.ContainsKey).ToDictionary(name => name, name => named[name], StringComparer.Ordinal);

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
