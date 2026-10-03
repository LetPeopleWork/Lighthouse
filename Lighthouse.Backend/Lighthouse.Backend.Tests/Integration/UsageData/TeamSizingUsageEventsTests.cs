using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// What sizing votes add to the usage data a consenting browser hands in. A vote cast and a Work Item
    /// the votes just made Ready are each one event carrying its name and one choice from a closed list:
    /// when it happened relative to the Team's Refinement. Until a Team has a Refinement cadence that choice
    /// is always "no cadence". Nothing about the Team, the Work Item, the answer or the voter travels.
    ///
    /// Written as what a browser posts and what reaches the collector, naming everything as text, because
    /// that is how it travels. Whether a browser that did not agree sends anything is the pipe's promise and
    /// is pinned once for every event by the fixtures beside this one.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("epic-5510-5881-refinement")]
    public class TeamSizingUsageEventsTests : UsageDataCollectorObservationTest
    {
        private const string PendingSlice11 = "Epic #5510 slice 11 (#6149) - pending DELIVER";

        private const string PendingSlice13 = "Epic #5510 slice 13 (#6151) - pending DELIVER";

        private const string TeamSizingVoteCast = "TeamSizingVoteCast";

        private const string TeamSizingReadinessReached = "TeamSizingReadinessReached";

        private const string TheLastEventNamedBeforeSizingVotes = "TeamRefinementConfigured";

        private const string NoCadence = "NoCadence";

        private const string SizingMomentOnTheWire = "sizing_moment";

        private static readonly string[] EverythingThePageSaysTravelsWithAMoment =
        [
            "version",
            "deployment_mode",
            "licence_tier",
            "auth_enabled",
            "$ip",
            "$geoip_disable",
            SizingMomentOnTheWire,
        ];

        // @driving_port @real-io @us-11 @slice-11 @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
        [TestCase(TeamSizingVoteCast, IgnoreReason = PendingSlice11)]
        [TestCase(TeamSizingReadinessReached, IgnoreReason = PendingSlice13)]
        public async Task A_browser_that_agreed_reports_the_event_with_its_name_and_when_it_happened_and_nothing_else(string name)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(name, $",\"sizingMoment\":\"{NoCadence}\""));
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(messages, Has.Count.EqualTo(1));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(name));
                Assert.That(PropertyNames(messages[0].GetProperty("properties")), Is.EquivalentTo(EverythingThePageSaysTravelsWithAMoment),
                    "nothing about the Team, the Work Item, the answer or the voter may travel with the event");
                Assert.That(messages[0].GetProperty("properties").GetProperty(SizingMomentOnTheWire).GetString(), Is.EqualTo(NoCadence));
            }
        }

        // @driving_port @real-io @us-11 @us-13 @error @contract-shape:unbounded-preservation
        // The moment is the whole reason these events exist; one arriving without it would be counted wrongly.
        [TestCase(TeamSizingVoteCast, IgnoreReason = PendingSlice11)]
        [TestCase(TeamSizingReadinessReached, IgnoreReason = PendingSlice13)]
        public async Task The_event_without_when_it_happened_is_refused(string name)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, name);

            using var withoutMoment = await HandInAsync(token, AnEvent(name));
            var forwarded = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutMoment.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(forwarded, Is.Empty);
            }
        }

        // @driving_port @real-io @us-11 @us-13 @error @contract-shape:unbounded-preservation
        [TestCase(TeamSizingVoteCast, ",\"route\":\"" + TeamMetricsTab + "\"", IgnoreReason = PendingSlice11)]
        [TestCase(TeamSizingVoteCast, ",\"workTrackingSystem\":\"Jira\"", IgnoreReason = PendingSlice11)]
        [TestCase(TeamSizingVoteCast, ",\"enabled\":true", IgnoreReason = PendingSlice11)]
        [TestCase(TeamSizingReadinessReached, ",\"route\":\"" + TeamMetricsTab + "\"", IgnoreReason = PendingSlice13)]
        public async Task The_event_carrying_anything_more_is_refused(string name, string somethingExtra)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, name);

            using var withExtra = await HandInAsync(token, AnEvent(name, $",\"sizingMoment\":\"{NoCadence}\"{somethingExtra}"));
            var forwarded = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withExtra.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(forwarded, Is.Empty);
            }
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        // The moment is a choice from a closed list; anything else could carry a date or a name.
        [TestCase("Tuesday")]
        [TestCase("2026-10-06")]
        [Ignore(PendingSlice11)]
        public async Task A_vote_cast_at_a_moment_not_on_the_list_is_refused(string moment)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, TeamSizingVoteCast);

            using var refused = await HandInAsync(token, AnEvent(TeamSizingVoteCast, $",\"sizingMoment\":\"{moment}\""));
            var forwarded = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(forwarded, Is.Empty);
            }
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        // Only the sizing events may say when something happened relative to a Refinement.
        [Test]
        [Ignore(PendingSlice11)]
        public async Task Another_event_carrying_a_sizing_moment_is_refused()
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, TeamSizingVoteCast);

            using var refused = await HandInAsync(token, AnEvent(TheLastEventNamedBeforeSizingVotes, $",\"sizingMoment\":\"{NoCadence}\""));
            var forwarded = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(forwarded, Is.Empty);
            }
        }

        /// <summary>
        /// The name is the contract and the number is whatever is next when the slice lands, so this pins
        /// the order the names were appended in rather than their numbers.
        /// </summary>
        // @us-11 @us-13 @contract-shape:bounded-change
        [TestCase(TeamSizingVoteCast, TheLastEventNamedBeforeSizingVotes, IgnoreReason = PendingSlice11)]
        [TestCase(TeamSizingReadinessReached, TeamSizingVoteCast, IgnoreReason = PendingSlice13)]
        public void The_sizing_events_are_appended_to_the_list_of_names_never_inserted(string name, string namedBefore)
        {
            var vocabulary = typeof(Backend.Program).Assembly.GetTypes()
                .First(type => type.IsEnum && type.Name.Equals("UsageDataEventName", StringComparison.Ordinal));
            var named = Enum.GetNames(vocabulary).Contains(name);
            var earlierIsNamed = Enum.GetNames(vocabulary).Contains(namedBefore);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(named, Is.True, $"{name} is not on the list of names this product can send");
                Assert.That(
                    named && earlierIsNamed ? (int)Enum.Parse(vocabulary, name) - (int)Enum.Parse(vocabulary, namedBefore) : 0,
                    Is.Positive,
                    $"{name} must come after {namedBefore}");
            }
        }

        private async Task ThePlainEventIsAccepted(string token, string name)
        {
            using var complete = await HandInAsync(token, AnEvent(name, $",\"sizingMoment\":\"{NoCadence}\""));
            await EverythingTheCollectorReceived();
            Outbound.Clear();

            Assert.That(complete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                "the event with its moment is accepted, so the refusal that follows is about what differs");
        }

        private static string AnEvent(string name, string parts = "")
            => $"{{\"events\":[{{\"name\":\"{name}\"{parts},\"offsetMs\":0,\"sequence\":0}}]}}";

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
    }
}
