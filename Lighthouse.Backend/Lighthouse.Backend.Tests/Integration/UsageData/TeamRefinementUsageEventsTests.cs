using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// What the Refinement tab adds to the usage data a consenting browser hands in. Choosing a Team's
    /// refinement states for the first time is one event carrying its name and nothing else - not which
    /// Team, not which states. Opening the Refinement tab is the existing tab opening, naming one more tab.
    ///
    /// Written as what a browser posts and what reaches the collector, naming the event and the tab as
    /// text, because that is how they travel. Whether a browser that did not agree sends anything is the
    /// pipe's promise and is pinned once for every event by the fixtures beside this one.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("epic-5510-5881-refinement")]
    public class TeamRefinementUsageEventsTests : UsageDataCollectorObservationTest
    {
        private const string TeamRefinementConfigured = "TeamRefinementConfigured";

        private const string TheLastEventNamedBeforeThisFeature = "TeamForecastRealityCheckRun";

        private const string TeamRefinementTab = "TeamDetail_Refinement";

        private const string TheRefinementTabsPublishedAddress = "/teams/:id/refinement";

        private const string UsageDataPage = "docs/settings/usagedata.md";

        private static readonly string[] EverythingThePageSaysTravels =
        [
            "version",
            "deployment_mode",
            "licence_tier",
            "auth_enabled",
            "$ip",
            "$geoip_disable",
        ];

        // @driving_port @real-io @us-01 @slice-01 @kpi-OUT-5510-K1-refinement-set-up @contract-shape:bounded-change
        [Test]
        public async Task A_browser_that_agreed_reports_refinement_being_set_up_as_one_event_carrying_only_its_name()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, RefinementBeingSetUp());
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(messages, Has.Count.EqualTo(1));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(TeamRefinementConfigured));
                Assert.That(PropertyNames(messages[0].GetProperty("properties")), Is.EquivalentTo(EverythingThePageSaysTravels),
                    "nothing about the Team or the states it chose may travel with the event");
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        [TestCase(",\"route\":\"" + TeamMetricsTab + "\"")]
        [TestCase(",\"workTrackingSystem\":\"Jira\"")]
        public async Task A_refinement_set_up_event_carrying_anything_but_its_name_is_refused(string somethingExtra)
        {
            var token = await ABrowserThatAgreedAsync();
            using var complete = await HandInAsync(token, RefinementBeingSetUp());
            await EverythingTheCollectorReceived();
            Outbound.Clear();

            using var withExtra = await HandInAsync(token, RefinementBeingSetUp(somethingExtra));
            var forwarded = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(complete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "the plain event is accepted, so the refusal below is about the extra and not about the name");
                Assert.That(withExtra.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(forwarded, Is.Empty);
            }
        }

        /// <summary>
        /// The name is the contract and the number is whatever is next when the slice lands, so this pins
        /// that it was appended after every name that existed before this feature rather than a number.
        /// </summary>
        // @us-01 @slice-01 @kpi-OUT-5510-K1-refinement-set-up @contract-shape:bounded-change
        [Test]
        public void Refinement_being_set_up_is_appended_to_the_list_of_names_never_inserted()
        {
            var vocabulary = typeof(Backend.Program).Assembly.GetTypes()
                .First(type => type.IsEnum && type.Name.Equals("UsageDataEventName", StringComparison.Ordinal));
            var named = Enum.GetNames(vocabulary).Contains(TeamRefinementConfigured);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Is.True, $"{TeamRefinementConfigured} is not on the list of names this product can send");
                Assert.That(
                    named ? (int)Enum.Parse(vocabulary, TeamRefinementConfigured) : -1,
                    Is.GreaterThan((int)Enum.Parse(vocabulary, TheLastEventNamedBeforeThisFeature)));
            }
        }

        // @driving_port @real-io @us-02 @slice-02 @kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:bounded-change
        [Test]
        public async Task A_browser_that_agreed_reports_opening_the_Refinement_tab_as_a_Team_tab_opening_naming_that_tab()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOf(TabOpened, TeamRefinementTab));
            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sent, Does.Contain(TabOpened));
                Assert.That(sent, Does.Contain(TheRefinementTabsPublishedAddress));
            }
        }

        // @us-02 @slice-02 @kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:bounded-change
        [Test]
        public void The_usage_data_page_lists_the_Refinement_tab_among_the_addresses_it_publishes()
        {
            var page = File.ReadAllText(Path.Combine(RepositoryRoot(), UsageDataPage.Replace('/', Path.DirectorySeparatorChar)));

            Assert.That(page, Does.Contain(TheRefinementTabsPublishedAddress),
                "a tab whose opening is reported ahead of the page listing it means data leaves that nobody was told about");
        }

        private static string RefinementBeingSetUp(string somethingExtra = "")
            => $"{{\"events\":[{{\"name\":\"{TeamRefinementConfigured}\"{somethingExtra},\"offsetMs\":0,\"sequence\":0}}]}}";

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
