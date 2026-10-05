using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// What the refinement need adds to the usage data a consenting browser hands in. Once a Team has a
    /// Refinement cadence, the two sizing events say whether they happened on a Refinement day or another
    /// day. And when the tab is opened on a Refinement day it reports which verdict it showed - below, in or
    /// above the range, or none - as one event carrying that choice from a closed list and nothing else.
    /// That is how anyone can tell whether Teams arrive at their Refinement with enough refined.
    ///
    /// Written as what a browser posts and what reaches the collector, naming everything as text, because
    /// that is how it travels.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("epic-5510-5881-refinement")]
    public class TeamRefinementNeedUsageEventsTests : UsageDataCollectorObservationTest
    {
        private const string PendingSlice04 = "Epic #5881 slice 04 (#6142) - pending DELIVER";

        private const string PendingSlice05 = "Epic #5881 slice 05 (#6143) - pending DELIVER";

        private const string TeamSizingVoteCast = "TeamSizingVoteCast";

        private const string TeamSizingReadinessReached = "TeamSizingReadinessReached";

        private const string TeamRefinementDayVerdictShown = "TeamRefinementDayVerdictShown";

        private const string NoCadence = "NoCadence";

        private const string OnRefinementDay = "OnRefinementDay";

        private const string OnOtherDay = "OnOtherDay";

        private const string SizingMomentOnTheWire = "sizing_moment";

        private const string VerdictOnTheWire = "refinement_verdict";

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

        private static readonly string[] EverythingThePageSaysTravelsWithAVerdict =
        [
            "version",
            "deployment_mode",
            "licence_tier",
            "auth_enabled",
            "$ip",
            "$geoip_disable",
            VerdictOnTheWire,
        ];

        // --- When a vote happened (slice 04) ---

        // @driving_port @real-io @us-04 @us-11 @slice-04 @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
        [TestCase(TeamSizingVoteCast, OnRefinementDay)]
        [TestCase(TeamSizingVoteCast, OnOtherDay)]
        [TestCase(TeamSizingReadinessReached, OnRefinementDay)]
        [TestCase(TeamSizingReadinessReached, OnOtherDay)]
        public async Task A_sizing_event_says_whether_it_happened_on_a_Refinement_day_and_nothing_else(string name, string moment)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(name, $",\"sizingMoment\":\"{moment}\""));
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            await ThenOneEventReachedTheCollectorCarrying(handedIn, messages, name, EverythingThePageSaysTravelsWithAMoment, SizingMomentOnTheWire, moment);
        }

        // @us-04 @slice-04 @contract-shape:bounded-change
        // The moments are appended to their list, so a number already counted never changes meaning.
        [TestCase(OnRefinementDay, NoCadence)]
        [TestCase(OnOtherDay, OnRefinementDay)]
        public void The_new_moments_are_appended_to_their_list_never_inserted(string moment, string namedBefore)
            => ThenIsAppendedAfter("UsageDataSizingMoment", moment, namedBefore);

        // --- Which verdict a Refinement day showed (slice 05) ---

        // @driving_port @real-io @us-05 @slice-05 @kpi-OUT-5510-K3-in-range-on-refinement-day @contract-shape:bounded-change
        [TestCase("Below")]
        [TestCase("In")]
        [TestCase("Above")]
        [TestCase("None")]
        public async Task A_browser_that_agreed_reports_the_verdict_a_Refinement_day_showed_and_nothing_else(string verdict)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, AnEvent(TeamRefinementDayVerdictShown, $",\"refinementVerdict\":\"{verdict}\""));
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            await ThenOneEventReachedTheCollectorCarrying(handedIn, messages, TeamRefinementDayVerdictShown, EverythingThePageSaysTravelsWithAVerdict, VerdictOnTheWire, verdict);
        }

        // @driving_port @real-io @us-05 @slice-05 @error @contract-shape:unbounded-preservation
        // The verdict is the whole reason the event exists; without it, or with anything beside it, it is refused.
        [TestCase("")]
        [TestCase(",\"refinementVerdict\":\"Mostly\"")]
        [TestCase(",\"refinementVerdict\":\"5-8\"")]
        [TestCase(",\"refinementVerdict\":\"In\",\"sizingMoment\":\"" + OnRefinementDay + "\"")]
        [TestCase(",\"refinementVerdict\":\"In\",\"route\":\"" + TeamMetricsTab + "\"")]
        [Ignore(PendingSlice05)]
        public async Task The_verdict_event_without_a_verdict_from_the_list_or_carrying_anything_more_is_refused(string parts)
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, AnEvent(TeamRefinementDayVerdictShown, ",\"refinementVerdict\":\"In\""));

            using var refused = await HandInAsync(token, AnEvent(TeamRefinementDayVerdictShown, parts));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        // @driving_port @real-io @us-05 @slice-05 @error @contract-shape:unbounded-preservation
        // Only the verdict event may carry a verdict.
        [Test]
        [Ignore(PendingSlice05)]
        public async Task A_sizing_event_carrying_a_verdict_is_refused()
        {
            var token = await ABrowserThatAgreedAsync();
            await ThePlainEventIsAccepted(token, AnEvent(TeamSizingVoteCast, $",\"sizingMoment\":\"{NoCadence}\""));

            using var refused = await HandInAsync(token, AnEvent(TeamSizingVoteCast, $",\"sizingMoment\":\"{NoCadence}\",\"refinementVerdict\":\"In\""));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        // @us-05 @slice-05 @contract-shape:bounded-change
        [Test]
        public void The_verdict_event_is_appended_to_the_list_of_names_never_inserted()
            => ThenIsAppendedAfter("UsageDataEventName", TeamRefinementDayVerdictShown, TeamSizingReadinessReached);

        // --- Steps ---

        private static async Task ThenOneEventReachedTheCollectorCarrying(
            HttpResponseMessage handedIn, List<JsonElement> messages, string name, string[] everythingThatTravels, string property, string value)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), await handedIn.Content.ReadAsStringAsync());
                Assert.That(messages, Has.Count.EqualTo(1));
            }

            var properties = messages[0].GetProperty("properties");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(name));
                Assert.That(PropertyNames(properties), Is.EquivalentTo(everythingThatTravels),
                    "nothing about the Team, its Work Items, the range or anybody may travel with the event");
                Assert.That(properties.GetProperty(property).GetString(), Is.EqualTo(value));
            }
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

        /// <summary>
        /// The name is the contract and the number is whatever is next when the slice lands, so this pins the
        /// order the names were appended in rather than their numbers.
        /// </summary>
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
                    Is.EqualTo(1),
                    $"{name} must come straight after {namedBefore}");
            }
        }

        private async Task ThePlainEventIsAccepted(string token, string body)
        {
            using var complete = await HandInAsync(token, body);
            await EverythingTheCollectorReceived();
            Outbound.Clear();

            Assert.That(complete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                "the complete event is accepted, so the refusal that follows is about what differs");
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
