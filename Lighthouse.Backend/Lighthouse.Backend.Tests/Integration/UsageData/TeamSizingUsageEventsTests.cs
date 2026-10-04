using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

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
        private const string PendingSlice13 = "Epic #5510 slice 13 (#6151) - pending DELIVER";

        private const string TeamSizingVoteCast = "TeamSizingVoteCast";

        private const string TeamSizingReadinessReached = "TeamSizingReadinessReached";

        private const string TheLastEventNamedBeforeSizingVotes = "TeamRefinementConfigured";

        private const string NoCadence = "NoCadence";

        private const string SizingMomentOnTheWire = "sizing_moment";

        private const string VoterKeyHeader = "X-Lighthouse-Voter-Key";

        private const string ForwardedForHeader = "X-Forwarded-For";

        private const string RefinementState = "Backlog";

        private const string WorkItemVotedOn = "GR-073";

        private const string DistinctiveVoterName = "Zephyrine Quillfeather";

        private const string DistinctiveVoterKey = "zq6152voterkey0000000000000000000000000000000000000000000000zq";

        private const string DistinctiveComment = "only if Zephyrine's export spike lands first";

        private const string DistinctiveAddress = "203.0.113.77";

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
        [TestCase(TeamSizingVoteCast)]
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

        // @driving_port @real-io @us-11 @us-13 @contract-shape:unbounded-preservation
        // No usage-data event ever carries personal data. The browser that just voted, under a name, a key, a
        // comment and an address nobody else uses, hands the event in with the same key and address on the
        // request; none of them may reach the collector, and neither may anything beyond the event's name,
        // its moment and the facts every event carries about the instance.
        [TestCase(TeamSizingVoteCast)]
        [TestCase(TeamSizingReadinessReached, IgnoreReason = PendingSlice13)]
        public async Task The_event_from_a_browser_that_just_voted_carries_nothing_about_the_voter(string name)
        {
            var token = await ABrowserThatAgreedAsync();
            using var vote = await TheBrowserVotesUnderItsNameKeyAndAddress(ATeamRefiningOneWorkItem());
            Assert.That(vote.IsSuccessStatusCode, Is.True,
                $"the vote was not taken, so the event cannot be shown to leave it behind: {(int)vote.StatusCode} {await vote.Content.ReadAsStringAsync()}");

            using var handedIn = await TheSameBrowserHandsIn(token, AnEvent(name, $",\"sizingMoment\":\"{NoCadence}\""));
            var sent = await EverythingTheCollectorReceived();
            var messages = EveryMessageIn(sent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(messages, Has.Count.EqualTo(1));
                Assert.That(sent, Does.Not.Contain(DistinctiveVoterName), "the voter's name travelled");
                Assert.That(sent, Does.Not.Contain(DistinctiveVoterKey), "the voter's key travelled");
                Assert.That(sent, Does.Not.Contain(DistinctiveComment), "the voter's comment travelled");
                Assert.That(sent, Does.Not.Contain(DistinctiveAddress), "the voter's address travelled");
                Assert.That(sent, Does.Not.Contain(WorkItemVotedOn), "the Work Item voted on travelled");
            }

            var properties = messages[0].GetProperty("properties");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(name));
                Assert.That(PropertyNames(properties), Is.EquivalentTo(EverythingThePageSaysTravelsWithAMoment));
                Assert.That(properties.GetProperty(SizingMomentOnTheWire).GetString(), Is.EqualTo(NoCadence));
                Assert.That(properties.GetProperty("$ip").ValueKind, Is.EqualTo(JsonValueKind.Null), "no address is sent for the collector to keep");
            }
        }

        // @driving_port @real-io @us-11 @us-13 @error @contract-shape:unbounded-preservation
        // The moment is the whole reason these events exist; one arriving without it would be counted wrongly.
        [TestCase(TeamSizingVoteCast)]
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
        [TestCase(TeamSizingVoteCast, ",\"route\":\"" + TeamMetricsTab + "\"")]
        [TestCase(TeamSizingVoteCast, ",\"workTrackingSystem\":\"Jira\"")]
        [TestCase(TeamSizingVoteCast, ",\"enabled\":true")]
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
        [TestCase(TeamSizingVoteCast, TheLastEventNamedBeforeSizingVotes)]
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

        /// <summary>The refinement scenarios run as a premium instance; the vote here is cast on the same terms.</summary>
        protected override void AlsoRegister(IServiceCollection services)
        {
            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

            services.RemoveAll<ILicenseService>();
            services.AddScoped(_ => licenseService.Object);
        }

        /// <summary>
        /// A Team that refines in Backlog, with one Work Item there to vote on, on an instance holding the
        /// settings every real one is started with - taking a vote reads them.
        /// </summary>
        private int ATeamRefiningOneWorkItem()
        {
            using var scope = Factory.Services.CreateScope();

            scope.ServiceProvider.GetServices<ISeeder>().OfType<AppSettingSeeder>().Single().Seed().GetAwaiter().GetResult();

            var team = new Team
            {
                Name = "Team Gravity",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Gravity's tracker",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DataRetrievalValue = "project = GRAVITY",
                WorkItemTypes = ["User Story"],
                ToDoStates = [RefinementState],
                DoingStates = ["Implementation"],
                DoneStates = ["Done"],
                RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = RefinementState }] },
            };

            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();

            var workItems = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
            workItems.Add(new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = WorkItemVotedOn,
                Name = "Configuration management",
                Type = "User Story",
                State = RefinementState,
                StateCategory = StateCategories.ToDo,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                Order = "1",
                Url = $"https://tracker.example/browse/{WorkItemVotedOn}",
            });
            workItems.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private async Task<HttpResponseMessage> TheBrowserVotesUnderItsNameKeyAndAddress(int teamId)
        {
            var vote = new JsonObject
            {
                ["answer"] = "YesBut",
                ["channel"] = "Web",
                ["voterName"] = DistinctiveVoterName,
                ["comment"] = DistinctiveComment,
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/latest/teams/{teamId}/refinement/work-items/{WorkItemVotedOn}/votes")
            {
                Content = new StringContent(vote.ToJsonString(), Encoding.UTF8, JsonMediaType),
            };
            request.Headers.Add(VoterKeyHeader, DistinctiveVoterKey);
            request.Headers.Add(ForwardedForHeader, DistinctiveAddress);

            return await Client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> TheSameBrowserHandsIn(string token, string body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EventsRoute)
            {
                Content = new StringContent(body, Encoding.UTF8, JsonMediaType),
            };
            request.Headers.Add(ConsentTokenHeader, token);
            request.Headers.Add(VoterKeyHeader, DistinctiveVoterKey);
            request.Headers.Add(ForwardedForHeader, DistinctiveAddress);

            return await Client.SendAsync(request);
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
