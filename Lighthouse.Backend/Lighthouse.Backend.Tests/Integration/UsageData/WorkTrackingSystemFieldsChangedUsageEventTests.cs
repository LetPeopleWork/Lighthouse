using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// What a connection's Additional Fields being changed adds to the usage data a consenting browser
    /// hands in. When the admin saves a field change, the connection page asks whether to update the
    /// Teams and Portfolios on the connection now or leave them to their next scheduled update; the
    /// answer travels as one event saying which, from a closed list of two, and nothing else - not the
    /// connection, not the field, not how many Teams or Portfolios there are.
    ///
    /// Written as what a browser posts and what reaches the collector, naming everything as text,
    /// because that is how it travels.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("story-6307-refresh-on-additional-fields-change")]
    [Category("slice-03")]
    public class WorkTrackingSystemFieldsChangedUsageEventTests : UsageDataCollectorObservationTest
    {
        private const string FieldsChanged = "WorkTrackingSystemFieldsChanged";

        private const string Now = "Now";

        private const string Later = "Later";

        private const string UpdatesOnTheWire = "updates";

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
        };

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

        private static readonly string[] EverythingThePageSaysTravelsWithTheChoice = [.. EverythingThePageSaysTravels, UpdatesOnTheWire];

        [TestCase(Now)]
        [TestCase(Later)]
        [Category("us-03")]
        [Category("driving_port")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        public async Task A_change_to_a_connections_fields_travels_with_when_the_updates_run_and_nothing_else(string choice)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(FieldsChanged, TheChoice(choice)));
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
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(FieldsChanged));
                Assert.That(PropertyNames(properties), Is.EquivalentTo(EverythingThePageSaysTravelsWithTheChoice),
                    "nothing about the connection, its fields or its Teams and Portfolios may travel with the event");
                Assert.That(properties.GetProperty(UpdatesOnTheWire).GetString(), Is.EqualTo(choice));
            }
        }

        // The choice is the whole of what the event says: without one from the list, or with anything
        // beside it, it is refused.
        [TestCase("")]
        [TestCase(",\"updates\":\"Soon\"")]
        [TestCase(",\"updates\":\"Now\",\"reportTemplate\":\"ThenAndNow\"")]
        [TestCase(",\"updates\":\"Later\",\"workTrackingSystem\":\"Jira\"")]
        [TestCase(",\"updates\":\"Now\",\"route\":\"TeamDetail_Metrics\"")]
        [Category("us-03")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_change_to_a_connections_fields_without_a_choice_from_the_list_or_carrying_anything_more_is_refused(string parts)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, AnEvent(FieldsChanged, TheChoice(Now)));

            using var refused = await HandInAsync(token, AnEvent(FieldsChanged, parts));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        // Only a change to a connection's fields may say when the updates run.
        [TestCase("TeamCreated")]
        [TestCase("WorkTrackingSystemConnected")]
        [Category("us-03")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Any_other_event_carrying_the_choice_is_refused(string name)
        {
            var token = await ABrowserThatAgreedAsync();
            var whatItCarries = WhatTheEventCarriesOfItsOwn(name);
            await ThePlainEventIsAccepted(token, AnEvent(name, whatItCarries));

            using var refused = await HandInAsync(token, AnEvent(name, whatItCarries + TheChoice(Later)));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        /// <summary>
        /// The name is the contract. Whatever number it gets, every event named before it keeps its own.
        /// </summary>
        [Test]
        [Category("us-03")]
        [Category("contract-shape:bounded-change")]
        public void The_event_is_named_and_every_event_named_before_it_keeps_its_number()
        {
            var named = TheEventsUsageDataNames();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named.Keys, Does.Contain(FieldsChanged));
                Assert.That(TheNumbersOf(named, EveryEventNamedBeforeIt.Keys), Is.EquivalentTo(EveryEventNamedBeforeIt),
                    "an event named before this one changed its number");
            }
        }

        // Data leaving that the page does not describe is data nobody was told about.
        [Test]
        [Category("us-03")]
        [Category("contract-shape:bounded-change")]
        public void The_usage_data_page_has_a_row_for_a_change_to_a_connections_fields_naming_both_choices()
        {
            var rows = TheUsageDataPage().Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("| A connection's Additional Fields were changed |", StringComparison.Ordinal))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1), "the page has one row for the event");
                Assert.That(rows, Has.All.Contains($"`{Now}`").And.All.Contains($"`{Later}`"));
            }
        }

        // --- Steps ---

        private static string TheChoice(string choice) => $",\"{UpdatesOnTheWire}\":\"{choice}\"";

        private static string AnEvent(string name, string parts)
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

        private static List<JsonElement> EveryMessageIn(string sent)
        {
            if (string.IsNullOrEmpty(sent))
            {
                return [];
            }

            using var document = JsonDocument.Parse(sent);
            return [.. document.RootElement.EnumerateArray().Select(message => message.Clone())];
        }

        private static string WhatTheEventCarriesOfItsOwn(string name)
            => name == "WorkTrackingSystemConnected" ? ",\"workTrackingSystem\":\"Jira\"" : string.Empty;

        private static Dictionary<string, int> TheEventsUsageDataNames()
        {
            var vocabulary = typeof(Backend.Program).Assembly.GetTypes()
                .First(type => type.IsEnum && type.Name.Equals("UsageDataEventName", StringComparison.Ordinal));

            return Enum.GetNames(vocabulary).ToDictionary(name => name, name => (int)Enum.Parse(vocabulary, name), StringComparer.Ordinal);
        }

        private static Dictionary<string, int> TheNumbersOf(Dictionary<string, int> named, IEnumerable<string> names)
            => names.Where(named.ContainsKey).ToDictionary(name => name, name => named[name], StringComparer.Ordinal);

        private static List<string> PropertyNames(JsonElement carried)
            => [.. carried.EnumerateObject().Select(property => property.Name)];

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
