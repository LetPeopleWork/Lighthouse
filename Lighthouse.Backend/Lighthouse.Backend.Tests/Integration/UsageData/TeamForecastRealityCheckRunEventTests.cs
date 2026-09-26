using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Acceptance scenarios for the event that says somebody ran a Forecast Reality Check and got an
    /// answer back. It carries its name and nothing else: not which Team, not its sampling window, not
    /// what the check found.
    ///
    /// Written as what a browser posts and what reaches the collector, for the reason the fixture beside
    /// this one gives. The name does not exist in the code yet and must not appear there before the
    /// usage data page describes it - so these name it as text, the assembly keeps building, and each
    /// scenario fails on its own assertion the moment it is un-ignored. When the name ships it also joins
    /// the list of name-only events in Slice04ProductEventsTests, whose sweeps then cover it too.
    ///
    /// Every scenario ships ignored and is switched on as a step of the work.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("epic-4172-forecast-backtest-sweep")]
    public class TeamForecastRealityCheckRunEventTests : UsageDataCollectorObservationTest
    {
        private const string Pending = "Pending: the Forecast Reality Check is not built yet (epic 4172, slice 01, story 6072).";

        private const string TeamForecastRealityCheckRun = "TeamForecastRealityCheckRun";

        /// <summary>Appended to the end of the list, never renumbered: a renumbered member silently names a different event.</summary>
        private const int ItsPlaceInTheList = 11;

        private const string ItsLineOnThePage = "A forecast reality check was run";

        private const string UsageDataPage = "docs/settings/usagedata.md";

        /// <summary>What the usage data page says travels with every event. This one adds nothing to it.</summary>
        private static readonly string[] EverythingThePageSaysTravels =
        [
            "version",
            "deployment_mode",
            "licence_tier",
            "auth_enabled",
            "$ip",
            "$geoip_disable",
        ];

        // @driving_port @real-io @us-01 @kpi-OUT-4172-reality-check-used-outside-the-vendor @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task A_browser_that_agreed_reports_a_reality_check_as_one_event_carrying_only_its_name()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ARealityCheckRun());
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(messages, Has.Count.EqualTo(1), "one check that came back is one event - no more, and not none");
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(TeamForecastRealityCheckRun));
                Assert.That(PropertyNames(messages[0].GetProperty("properties")), Is.EquivalentTo(EverythingThePageSaysTravels),
                    "nothing about the Team, its window or what the check found may travel with the event");
            }
        }

        // @driving_port @real-io @us-01 @error @contract-shape:unbounded-preservation
        [TestCase("declined")]
        [TestCase(null)]
        [Ignore(Pending)]
        public async Task Nothing_leaves_a_browser_that_did_not_agree_when_it_runs_a_reality_check(string? decision)
        {
            var agreed = await ABrowserThatAgreedAsync();
            using var control = await HandInAsync(agreed, ARealityCheckRun());
            var fromTheAgreedBrowser = EveryMessageIn(await EverythingTheCollectorReceived());
            Outbound.Clear();

            var token = decision is null ? null : await ADecisionRecordedAsync(decision);
            using var handedIn = await HandInAsync(token, ARealityCheckRun());
            var fromThisBrowser = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromTheAgreedBrowser, Has.Count.EqualTo(1),
                    "the agreed browser's check sent nothing, so 'nothing left the other browser' would hold for a pipe that never carries this event");
                Assert.That(fromThisBrowser, Is.Empty);
            }
        }

        // @driving_port @real-io @us-01 @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task Nothing_is_forwarded_while_the_administrator_has_stopped_usage_data()
        {
            var token = await ABrowserThatAgreedAsync();
            using var control = await HandInAsync(token, ARealityCheckRun());
            var beforeTheVeto = EveryMessageIn(await EverythingTheCollectorReceived());
            Outbound.Clear();

            StoreTheVeto(engaged: true);
            using var handedIn = await HandInAsync(token, ARealityCheckRun());
            var underTheVeto = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(beforeTheVeto, Has.Count.EqualTo(1));
                Assert.That(underTheVeto, Is.Empty);
            }
        }

        /// <summary>
        /// The event is name-only by decision. A message under its name that carries any of the parts
        /// other events are allowed - a page, a kind of system, a setting - is refused whole rather than forwarded with the extra dropped.
        /// </summary>
        // @driving_port @real-io @us-01 @error @contract-shape:unbounded-preservation
        [TestCase(",\"route\":\"" + TeamMetricsTab + "\"")]
        [TestCase(",\"workTrackingSystem\":\"Jira\"")]
        [TestCase(",\"optionalFeature\":\"FeatureOrder\",\"enabled\":true")]
        [Ignore(Pending)]
        public async Task A_reality_check_event_carrying_anything_but_its_name_is_refused(string somethingExtra)
        {
            var token = await ABrowserThatAgreedAsync();
            using var complete = await HandInAsync(token, ARealityCheckRun());
            Outbound.Clear();

            using var withExtra = await HandInAsync(token, ARealityCheckRun(somethingExtra));
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
        /// The list in the code and the list on the page are one promise. This pins the new member's
        /// name and number and its line on the page; the shipped disclosure test then counts the two
        /// lists against each other.
        /// </summary>
        // @us-01 @kpi-OUT-4172-reality-check-used-outside-the-vendor @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public void The_event_is_the_twelfth_on_the_list_and_the_usage_data_page_describes_it()
        {
            var vocabulary = typeof(Backend.Program).Assembly.GetTypes()
                .First(type => type.IsEnum && type.Name.Equals("UsageDataEventName", StringComparison.Ordinal));
            var named = Enum.GetNames(vocabulary).Contains(TeamForecastRealityCheckRun);
            var page = File.ReadAllText(Path.Combine(RepositoryRoot(), UsageDataPage.Replace('/', Path.DirectorySeparatorChar)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Is.True, $"{TeamForecastRealityCheckRun} is not on the list of names this product can send");
                Assert.That(named ? (int)Enum.Parse(vocabulary, TeamForecastRealityCheckRun) : -1, Is.EqualTo(ItsPlaceInTheList));
                Assert.That(page, Does.Contain(ItsLineOnThePage),
                    "an event that ships ahead of the line describing it means data leaves that nobody was told about");
            }
        }

        private static string ARealityCheckRun(string somethingExtra = "")
            => $"{{\"events\":[{{\"name\":\"{TeamForecastRealityCheckRun}\"{somethingExtra},\"offsetMs\":0,\"sequence\":0}}]}}";

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
