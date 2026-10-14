using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// What Reports add to the usage data a consenting browser hands in. A report being created and a report
    /// being opened each travel as one event carrying which type of report it was, from a closed list, and
    /// nothing else - not its name, its owner, its dates or its numbers. A report being deleted travels as its
    /// name alone. Opening the Reports tab is the existing tab opening, naming one more tab of a Team or a
    /// Portfolio.
    ///
    /// Written as what a browser posts and what reaches the collector, naming everything as text, because that
    /// is how it travels. Whether a browser that did not agree sends anything is the pipe's promise and is
    /// pinned once for every event by the fixtures beside this one.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("epic-5878-baseline")]
    public class ReportUsageEventsTests : UsageDataCollectorObservationTest
    {
        private const string PendingSlice08 = "Epic #5878 slice 08 (#6166) - pending DELIVER";

        private const string ReportCreated = "ReportCreated";

        private const string ReportOpened = "ReportOpened";

        private const string ReportDeleted = "ReportDeleted";

        private const string ThenAndNow = "ThenAndNow";

        private const string TemplateOnTheWire = "report_template";

        private const string TheLastEventNamedBeforeReports = "TeamRefinementDayVerdictShown";

        private const string TeamReportsTab = "TeamDetail_Reports";

        private const string PortfolioReportsTab = "PortfolioDetail_Reports";

        private const string PortfolioTabOpened = "PortfolioTabOpened";

        private const string UsageDataPage = "docs/settings/usagedata.md";

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

        // @driving_port @real-io @us-01 @slice-01 @kpi-OUT-5878-K1-report-created @kpi-OUT-5878-K2-reports-reopened @contract-shape:bounded-change
        [TestCase(ReportCreated)]
        [TestCase(ReportOpened)]
        public async Task A_report_created_or_opened_travels_with_its_type_and_nothing_else(string name)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(name, $",\"reportTemplate\":\"{ThenAndNow}\""));
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());
            var answer = await handedIn.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), answer);
                Assert.That(messages, Has.Count.EqualTo(1));
            }

            var properties = messages[0].GetProperty("properties");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(name));
                Assert.That(PropertyNames(properties), Is.EquivalentTo(EverythingThePageSaysTravelsWithATemplate),
                    "nothing about the report, its owner, its windows or its numbers may travel with the event");
                Assert.That(properties.GetProperty(TemplateOnTheWire).GetString(), Is.EqualTo(ThenAndNow));
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        // The type is the whole of what these two events say; without one from the list, or with anything beside it, they are refused.
        [TestCase(ReportCreated, "")]
        [TestCase(ReportCreated, ",\"reportTemplate\":\"Signal\"")]
        [TestCase(ReportCreated, ",\"reportTemplate\":\"then-and-now\"")]
        [TestCase(ReportCreated, ",\"reportTemplate\":\"ThenAndNow\",\"route\":\"" + TeamReportsTab + "\"")]
        [TestCase(ReportOpened, "")]
        [TestCase(ReportOpened, ",\"reportTemplate\":\"ThenAndNow\",\"refinementVerdict\":\"In\"")]
        public async Task A_report_event_without_a_type_from_the_list_or_carrying_anything_more_is_refused(string name, string parts)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, AnEvent(name, $",\"reportTemplate\":\"{ThenAndNow}\""));

            using var refused = await HandInAsync(token, AnEvent(name, parts));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        // Only a report being created or opened may say which type it was.
        [Test]
        public async Task A_tab_opening_carrying_a_report_type_is_refused()
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, ABatchOf(TabOpened, TeamMetricsTab));

            using var refused = await HandInAsync(token,
                $"{{\"events\":[{{\"name\":\"{TabOpened}\",\"route\":\"{TeamMetricsTab}\",\"reportTemplate\":\"{ThenAndNow}\",\"offsetMs\":0,\"sequence\":0}}]}}");

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        // @driving_port @real-io @us-01 @us-06 @slice-01 @slice-06 @kpi-OUT-5878-K1-report-created @contract-shape:bounded-change
        [TestCase(TabOpened, TeamReportsTab, "/teams/:id/reports")]
        [TestCase(PortfolioTabOpened, PortfolioReportsTab, "/portfolios/:id/reports")]
        public async Task Opening_the_Reports_tab_travels_as_a_tab_opening_naming_that_tab(string name, string tab, string publishedAddress)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOf(name, tab));
            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sent, Does.Contain(name));
                Assert.That(sent, Does.Contain(publishedAddress));
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @kpi-OUT-5878-guardrail-quick-deletes @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice08)]
        public async Task A_report_deleted_travels_as_its_name_alone()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(ReportDeleted));
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(messages, Has.Count.EqualTo(1));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(ReportDeleted));
                Assert.That(PropertyNames(messages[0].GetProperty("properties")), Is.EquivalentTo(EverythingThePageSaysTravels));
            }
        }

        // @driving_port @real-io @us-08 @slice-08 @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice08)]
        public async Task A_report_deleted_carrying_its_type_is_refused()
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, AnEvent(ReportDeleted));

            using var refused = await HandInAsync(token, AnEvent(ReportDeleted, $",\"reportTemplate\":\"{ThenAndNow}\""));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        /// <summary>
        /// The name is the contract and the number is whatever is next when the slice lands, so this pins the
        /// order the names were appended in rather than their numbers.
        /// </summary>
        // @us-01 @us-08 @slice-01 @slice-08 @contract-shape:bounded-change
        [TestCase(ReportCreated, TheLastEventNamedBeforeReports)]
        [TestCase(ReportOpened, ReportCreated)]
        [TestCase(ReportDeleted, ReportOpened, Ignore = PendingSlice08)]
        public void The_report_events_are_appended_to_the_list_of_names_never_inserted(string name, string namedBefore)
            => ThenIsAppendedAfter("UsageDataEventName", name, namedBefore);

        // @us-01 @slice-01 @contract-shape:bounded-change
        [Test]
        public void Then_and_Now_is_the_first_report_type_on_its_own_list()
        {
            var vocabulary = typeof(Backend.Program).Assembly.GetTypes()
                .FirstOrDefault(type => type.IsEnum && type.Name.Equals("UsageDataReportTemplate", StringComparison.Ordinal));

            var names = vocabulary is null ? [] : Enum.GetNames(vocabulary);

            Assert.That(names, Is.EqualTo(OnlyThenAndNow), "the list of report types usage data may name");
        }

        // @us-01 @us-06 @us-08 @slice-01 @slice-06 @slice-08 @contract-shape:bounded-change
        // Data leaving that the page does not describe is data nobody was told about. Each row is found by what
        // its first column says, and must name what travels with it.
        [TestCase("A report was created", "`ThenAndNow`")]
        [TestCase("A report was opened", "`ThenAndNow`")]
        [TestCase("Which type of report", "`ThenAndNow`")]
        [TestCase("A Team tab was opened", "seven Team tabs")]
        [TestCase("A Portfolio tab was opened", "six Portfolio tabs")]
        [TestCase("A report was deleted", "| Nothing", Ignore = PendingSlice08)]
        public void The_usage_data_page_has_a_row_for_what_Reports_send(string rowSays, string rowNames)
        {
            var rows = TheUsageDataPage().Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith($"| {rowSays} |", StringComparison.Ordinal))
                .ToList();

            Assert.That(rows, Has.Count.EqualTo(1), $"the page has one row starting \"{rowSays}\"");
            Assert.That(rows, Has.All.Contains(rowNames));
        }

        // @us-01 @us-06 @slice-01 @slice-06 @contract-shape:bounded-change
        [TestCase("/teams/:id/reports")]
        [TestCase("/portfolios/:id/reports")]
        public void The_usage_data_page_lists_the_Reports_address_among_those_it_publishes(string address)
        {
            Assert.That(TheUsageDataPage(), Does.Contain($"`{address}`"));
        }

        // --- Steps ---

        private static readonly string[] OnlyThenAndNow = [ThenAndNow];

        private static string AnEvent(string name, string parts = "")
            => $"{{\"events\":[{{\"name\":\"{name}\"{parts},\"offsetMs\":0,\"sequence\":0}}]}}";

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

        private static void ThenIsAppendedAfter(string list, string name, string namedBefore)
        {
            var vocabulary = typeof(Backend.Program).Assembly.GetTypes()
                .First(type => type.IsEnum && type.Name.Equals(list, StringComparison.Ordinal));
            var names = Enum.GetNames(vocabulary);
            var bothNamed = names.Contains(name) && names.Contains(namedBefore);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(names, Does.Contain(name), $"{name} is not on the list {list}");
                Assert.That(
                    bothNamed ? (int)Enum.Parse(vocabulary, name) - (int)Enum.Parse(vocabulary, namedBefore) : 0,
                    Is.GreaterThan(0),
                    $"{name} must come after {namedBefore}");
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

        private static string TheUsageDataPage()
            => File.ReadAllText(Path.Combine(RepositoryRoot(), UsageDataPage.Replace('/', Path.DirectorySeparatorChar)));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the docs read.");
            return Directory.GetParent(directory!.FullName)!.FullName;
        }
    }
}
