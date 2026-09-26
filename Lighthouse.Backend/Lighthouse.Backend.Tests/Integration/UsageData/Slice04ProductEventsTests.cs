using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5733 slice 04, ADO #5837) - the vocabulary stops being one
    /// navigation event. US-08 (AC-08.1, AC-08.2, AC-08.3).
    ///
    /// Every scenario here names its event as text rather than as a member of the list in the code,
    /// because that list does not exist yet and must not: a name added to it without a line on the
    /// usage data page is data leaving that nobody was told about, and the page and the list are
    /// counted against each other by a test that is already live. These post what a browser posts,
    /// and they start passing once the list and the page gain each name together.
    ///
    /// They read what did or did not reach the collector rather than what some component was asked
    /// to do, because "nothing was sent" is the outcome this feature degrades to on every failure -
    /// so a scenario that only checks a call was made cannot tell a working gate from a broken pipe.
    /// How that boundary is watched, and why nothing here can reach the real collector, lives in
    /// <see cref="UsageDataCollectorObservationTest"/>.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("slice-04")]
    public class Slice04ProductEventsTests : UsageDataCollectorObservationTest
    {
        private const string TeamTabOpened = "TeamTabOpened";
        private const string PortfolioTabOpened = "PortfolioTabOpened";
        private const string TheNameTheseTwoReplaced = "TeamOrPortfolioTabOpened";
        private const string PortfolioMetricsTab = "PortfolioDetail_Metrics";
        private const string WorkTrackingSystemConnected = "WorkTrackingSystemConnected";
        private const string OptionalFeatureToggled = "OptionalFeatureToggled";

        /// <summary>
        /// The events that say somebody used something rather than that somebody looked at
        /// something. None of them happens on a page whose address names one of the customer's own
        /// Teams or Portfolios, so none of them carries an address at all.
        /// </summary>
        private static readonly string[] EventsThatCarryNothingButTheirName =
        [
            "TeamCreated",
            "TeamDeleted",
            "PortfolioCreated",
            "PortfolioDeleted",
            "TeamManualForecastRun",
            "TeamRefreshTriggered",
            "PortfolioRefreshTriggered",
            "TeamForecastRealityCheckRun",
        ];

        /// <summary>
        /// Every name this product can send. The gates are meant to hold for all of them, and
        /// nothing about an event's shape is supposed to change that - which is only worth
        /// asserting if the awkwardly shaped ones are in the list rather than only the ones that
        /// carry nothing but their name.
        /// </summary>
        private static readonly string[] EveryEventThereIs =
        [
            TeamTabOpened,
            PortfolioTabOpened,
            .. EventsThatCarryNothingButTheirName,
            WorkTrackingSystemConnected,
            OptionalFeatureToggled,
        ];

        /// <summary>
        /// The instance facts the usage data page lists as travelling with every event, and the two
        /// instructions that keep the caller's own address out of what the collector stores.
        /// </summary>
        private static readonly string[] EverythingThePageSaysTravels =
        [
            "version",
            "deployment_mode",
            "licence_tier",
            "auth_enabled",
            "$ip",
            "$geoip_disable",
        ];

        [Test]
        public async Task A_team_tab_opening_arrives_as_the_address_this_product_publishes_for_it()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOf(TeamTabOpened, TeamMetricsTab));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sent, Does.Contain(TeamTabOpened));
                Assert.That(sent, Does.Contain("/teams/:id/metrics"));
            }
        }

        [Test]
        public async Task A_portfolio_tab_opening_arrives_as_the_address_this_product_publishes_for_it()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOf(PortfolioTabOpened, PortfolioMetricsTab));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sent, Does.Contain(PortfolioTabOpened));
                Assert.That(sent, Does.Contain("/portfolios/:id/metrics"));
            }
        }

        /// <summary>
        /// Saying which kind of page it was in the name, and saying it again in the address, means
        /// the two can disagree. A message claiming a Portfolio tab and naming a Team address would
        /// be counted as a Portfolio opening that never happened - a number quietly wrong rather
        /// than a message obviously broken, which is the kind nobody finds.
        /// </summary>
        [Test]
        [TestCase(PortfolioTabOpened, TeamMetricsTab)]
        [TestCase(TeamTabOpened, PortfolioMetricsTab)]
        public async Task A_tab_opening_that_names_one_kind_of_page_and_the_others_address_is_refused(
            string name, string address)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOf(name, address));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(sent, Is.Empty, "a message the product refused to read still reached the collector");
            }
        }

        /// <summary>
        /// The name this Epic shipped first said only that a view was opened, and said it for both
        /// kinds of page at once. Nothing was ever sent under it - it has never been in a published
        /// release - so it is replaced rather than kept alongside, and a browser still using it is
        /// running code this product no longer ships.
        /// </summary>
        [Test]
        public async Task The_single_name_these_two_replaced_is_no_longer_something_anyone_can_send()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOf(TheNameTheseTwoReplaced, TeamMetricsTab));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(sent, Is.Empty);
            }
        }

        [Test]
        [TestCaseSource(nameof(EventsThatCarryNothingButTheirName))]
        public async Task An_event_about_something_somebody_did_arrives_without_any_address(string name)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOfJustTheName(name));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sent, Does.Contain(name));
                Assert.That(WhatTravelledWithTheFirstMessageIn(sent), Does.Not.Contain("route"),
                    "an event about something somebody did carried an address, so the collector now "
                    + "holds a page opening nobody reported");
            }
        }

        /// <summary>
        /// An address on an event that happens on no particular page is worth refusing rather than
        /// ignoring: whoever sent it believed it would be counted, and a message half accepted is
        /// the one nobody notices.
        /// </summary>
        [Test]
        public async Task An_event_about_something_somebody_did_is_refused_if_it_carries_an_address()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOf("TeamCreated", TeamMetricsTab));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(sent, Is.Empty);
            }
        }

        [Test]
        [TestCase("AzureDevOps")]
        [TestCase("Jira")]
        [TestCase("Linear")]
        [TestCase("Csv")]
        [TestCase("ServiceNow")]
        public async Task Connecting_a_work_tracking_system_says_which_kind_it_was(string system)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(
                token, ABatchNamingAWorkTrackingSystem(WorkTrackingSystemConnected, system));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sent, Does.Contain(WorkTrackingSystemConnected));
                Assert.That(sent, Does.Contain(system));
            }
        }

        /// <summary>
        /// Which kind of system it was is the whole reason this event exists - it is what answers
        /// whether a connector somebody built is being used at all. One arriving without it would
        /// be counted as a connection of no particular kind, which is worse than not counting it.
        /// </summary>
        [Test]
        public async Task Connecting_a_work_tracking_system_without_saying_which_kind_is_refused()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOfJustTheName(WorkTrackingSystemConnected));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(sent, Is.Empty);
            }
        }

        [Test]
        public async Task A_kind_of_work_tracking_system_this_product_does_not_have_is_refused()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(
                token, ABatchNamingAWorkTrackingSystem(WorkTrackingSystemConnected, "Trello"));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(sent, Is.Empty);
            }
        }

        /// <summary>
        /// Each event may carry only what its own entry says it carries. Without that, the promise
        /// that no address travels with an event holds because every call site remembered - and one
        /// of them eventually will not.
        /// </summary>
        [Test]
        public async Task An_event_that_has_no_business_naming_a_work_tracking_system_is_refused_for_naming_one()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(
                token, ABatchNamingAWorkTrackingSystem("TeamCreated", "Jira"));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(sent, Is.Empty);
            }
        }

        /// <summary>
        /// The control, and it is not optional. Every scenario below whose point is that nothing was
        /// sent is worthless against a pipe that sends nothing anyway, and a pipe that sends nothing
        /// anyway looks exactly like a gate working perfectly.
        /// </summary>
        [Test]
        public async Task With_nothing_stopping_it_one_of_the_new_events_reaches_the_collector()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOfJustTheName("TeamCreated"));

            Assert.That(await EverythingTheCollectorReceived(), Does.Contain("TeamCreated"),
                "the batch this scenario handed in did not reach the collector, so every 'nothing "
                + "was sent' scenario below would hold against a pipe that carries none of these "
                + "events at all");
        }

        /// <summary>
        /// The administrator's switch was built before any of these events existed, and it has to
        /// cover them without having been told about them. A gate that must be remembered at each
        /// new call site is a gate that will be forgotten at one of them.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(EveryEventThereIs))]
        public async Task Nothing_leaves_an_instance_whose_administrator_stopped_usage_data(string name)
        {
            var token = await ABrowserThatAgreedAsync();
            StoreTheVeto(engaged: true);

            using var handedIn = await HandInAsync(token, ABatchOfWhateverShape(name));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "a browser is told the same thing whether its batch is kept or dropped, so that "
                    + "it cannot learn from the answer what this instance decided");
                Assert.That(sent, Is.Empty);
            }
        }

        [Test]
        [TestCaseSource(nameof(EveryEventThereIs))]
        public async Task Nothing_leaves_a_browser_that_refused(string name)
        {
            var token = await ABrowserThatRefusedAsync();

            using var handedIn = await HandInAsync(token, ABatchOfWhateverShape(name));

            Assert.That(await EverythingTheCollectorReceived(), Is.Empty);
        }

        /// <summary>
        /// The page the dialog links to is the only place the list of what is sent exists. This
        /// reads the message that actually left and checks every part of it against that list, so a
        /// field added in passing - one that looked harmless at the call site - fails here rather
        /// than in somebody else's database.
        /// </summary>
        [Test]
        public async Task Nothing_travels_with_an_event_beyond_what_the_page_says_travels()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ABatchOfJustTheName("TeamCreated"));

            var carried = WhatTravelledWithTheFirstMessageIn(await EverythingTheCollectorReceived());

            Assert.That(carried, Is.EquivalentTo(EverythingThePageSaysTravels),
                "something is travelling with an event that the usage data page does not describe");
        }

        /// <summary>
        /// A message this event would actually be sent in, whatever shape it has to be.
        ///
        /// The gates sit behind the reading, so a message the reader turns away never reaches them.
        /// A tab opening handed in without its page, or a connection without its kind, is refused
        /// for being malformed - and a scenario about the veto would then pass without the veto
        /// having done anything.
        /// </summary>
        private static string ABatchOfWhateverShape(string name) => name switch
        {
            TeamTabOpened => ABatchOf(name, TeamMetricsTab),
            PortfolioTabOpened => ABatchOf(name, PortfolioMetricsTab),
            WorkTrackingSystemConnected => ABatchNamingAWorkTrackingSystem(name, "Jira"),
            OptionalFeatureToggled => ABatchSwitchingASetting(name),
            _ => ABatchOfJustTheName(name),
        };

        private static string ABatchOfJustTheName(string name)
            => $"{{\"events\":[{{\"name\":\"{name}\",\"offsetMs\":0,\"sequence\":0}}]}}";

        private static string ABatchNamingAWorkTrackingSystem(string name, string system)
            => $"{{\"events\":[{{\"name\":\"{name}\",\"workTrackingSystem\":\"{system}\",\"offsetMs\":0,\"sequence\":0}}]}}";

        private static string ABatchSwitchingASetting(string name)
            => $"{{\"events\":[{{\"name\":\"{name}\",\"optionalFeature\":\"FeatureOrder\",\"enabled\":true,\"offsetMs\":0,\"sequence\":0}}]}}";

        private static List<string> WhatTravelledWithTheFirstMessageIn(string sent)
        {
            using var document = JsonDocument.Parse(sent);

            return [.. document.RootElement[0].GetProperty("properties")
                .EnumerateObject().Select(property => property.Name)];
        }
    }
}
