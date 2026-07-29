using System.Globalization;
using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5575, US-02 AC1 / AC5 / AC6 / AC7. The connector asking a ServiceNow instance for one
    // team's work, exercised against a stubbed transport that behaves the way the measured instance
    // behaves — offset paging with X-Total-Count, short pages, and sysparm_display_value=all.
    //
    // Layer 3 (real adapter, stubbed transport): sad paths are enumerated one example each, never
    // generated. The field-by-field mapping rules live in ServiceNowWorkItemMapperTest and the
    // query verdict's rungs in ServiceNowTeamQueryVerdictTest; this file is about what the connector
    // asks for and what it does with the answer.
    [TestFixture]
    public class ServiceNowTeamSyncTest
    {
        private const string InstanceUrl = "https://dev12345.service-now.com/";
        private const string TeamsOwnQuery = "assignment_group.name=Service Desk^active=true";

        // AC1. The query the flow coach wrote is the query that gets asked, against the table the
        // connection was configured for. Anything else and the team is looking at somebody else's work.
        [Test]
        public async Task SyncingATeam_AsksTheConfiguredTableForTheWorkTheFlowCoachDescribed()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam(query: TeamsOwnQuery, table: "change_request"));

            var asked = instance.Requests.Select(uri => Uri.UnescapeDataString(uri.AbsoluteUri)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asked, Has.Some.Contains("/api/now/table/change_request"));
                Assert.That(asked, Has.Some.Contains(TeamsOwnQuery),
                    "The flow coach's own query has to reach the instance verbatim.");
            }
        }

        // The replacement for the sys_choice lookup DESIGN named: display_value=all needs no extra
        // table access, works on a read-only account, and returns both forms of every field — the
        // label the flow coach maps and the universal time Throughput buckets by.
        [Test]
        public async Task SyncingATeam_AsksForBothTheLabelAndTheUnderlyingValueOfEveryField()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            var asked = instance.Requests.Select(uri => uri.AbsoluteUri).ToList();

            Assert.That(asked, Has.Some.Contains("sysparm_display_value=all"),
                "Without this, state comes back as a bare integer and there is no label to map.");
        }

        // AC7. The instance returns short pages regardless of what was asked for, and says how many
        // rows exist in X-Total-Count. A pager that trusts its own limit stops early and the team's
        // Throughput silently reads low.
        [Test]
        public async Task WorkSpreadAcrossMorePagesThanOne_IsAllBroughtBack()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 2);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(workItems.ToList(), Has.Count.EqualTo(5));
        }

        [Test]
        public async Task PagesOfWork_NeitherOverlapNorSkip()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 2);
            var subject = CreateSubject(instance);

            var referenceIds = (await subject.GetWorkItemsForTeam(ATeam())).Select(item => item.ReferenceId).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(referenceIds, Is.Unique);
                Assert.That(referenceIds, Is.EquivalentTo(new[] { "INC0000001", "INC0000002", "INC0000003", "INC0000004", "INC0000005" }));
            }
        }

        // SPIKE Q7 measured ~600ms per Table API call and no rate limiting, so the constraint is
        // wall-clock, not throttling. Five records must cost pages, not five round trips.
        [Test]
        public async Task SyncingATeam_ReadsInBatchesRatherThanOneRecordAtATime()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 2);
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(instance.Requests, Has.Count.LessThanOrEqualTo(3),
                "Five records over pages of two is three reads. Anything approaching one call per record is a five-minute sync on a real instance.");
        }

        // Linear's precedent: a team only sees work in the states it has mapped. An unmapped label
        // is work the flow coach never told Lighthouse how to interpret.
        [Test]
        public async Task WorkInAStateTheTeamNeverMapped_IsLeftOut()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(workItems.Select(item => item.ReferenceId), Has.No.Member("INC0000005"),
                "INC0000005 sits in 'Awaiting Vendor', which this team has not mapped to any of its own states.");
        }

        // The silent-filter trap's sibling. An unconfigured team must not degrade into an
        // unfiltered read, which is precisely how a team ends up reporting the whole instance.
        [Test]
        public async Task ATeamThatHasNotSaidWhichWorkIsTheirs_ReadsNothingRatherThanEverything()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam(query: string.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Empty);
                Assert.That(instance.Requests, Is.Empty,
                    "A team with no query must not ask the instance for anything, because asking with no query returns the whole table.");
            }
        }

        // AC5. ServiceNow cannot supply transition history on a read-only account, so the connector
        // must not invent any: WorkItemService's sync-delta fallback is what fills the gap, and it
        // only runs when the connector leaves the history empty and declares it unsupported.
        [Test]
        public async Task SyncedWork_CarriesNoInventedHistory()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False);
                Assert.That(workItems.SelectMany(item => item.SyncedTransitions), Is.Empty,
                    "A fabricated transition is worse than none: it would look like measured time-in-state and be a guess.");
            }
        }

        // AC2 end to end through the connector, because the mapper being right is worth nothing if
        // the connector reads the wrong form of the response.
        [Test]
        public async Task WorkThatWasResolvedButNeverClosed_ArrivesWithTheDayItFinished()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());
            var resolvedItem = workItems.SingleOrDefault(item => item.ReferenceId == "INC0000001");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolvedItem, Is.Not.Null);
                Assert.That(resolvedItem?.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 0, 25, 29, DateTimeKind.Utc)),
                    "resolved_at is set and closed_at is empty, and the universal form of resolved_at falls on the 30th.");
                Assert.That(resolvedItem?.State, Is.EqualTo("Resolved"),
                    "The label the service desk uses, not the choice value 6.");
            }
        }

        // AC6. The comparison IS the detection — one probe cannot tell a silently-widened query from
        // a correct one, because both answer 200 with rows.
        [Test]
        public async Task ValidatingATeamsSettings_ComparesWhatTheQuerySelectsAgainstWhatTheTableHolds()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10);
            var subject = CreateSubject(instance);

            await subject.ValidateTeamSettings(ATeam());

            var queries = instance.Requests.Select(uri => Uri.UnescapeDataString(uri.Query)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queries, Has.Some.Contains(TeamsOwnQuery),
                    "One probe asks what the flow coach's query selects.");
                Assert.That(queries, Has.Some.Matches<string>(query => !query.Contains(TeamsOwnQuery)),
                    "The other asks what the table holds without it. Without both counts there is nothing to compare.");
            }
        }

        [Test]
        public async Task ValidatingATeamThatHasNotSaidWhichWorkIsTheirs_AsksForAQueryWithoutContactingTheInstance()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(ATeam(query: string.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("missing_query"));
                Assert.That(instance.Requests, Is.Empty);
            }
        }

        // AC6's other half — an unresolvable table. Slice 01 already built this ladder; team
        // validation routes through it rather than inventing a second vocabulary for the same
        // failures.
        [Test]
        public async Task ValidatingATeamAgainstATableTheInstanceDoesNotHave_IsToldTheTableIsUnknown()
        {
            var subject = CreateSubject(AnInstanceThatAnswers(HttpStatusCode.BadRequest));

            var result = await subject.ValidateTeamSettings(ATeam(table: "no_such_table"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("unknown_table"));
            }
        }

        [Test]
        public async Task ValidatingATeamWithACredentialThatCannotReadTheTable_IsToldItIsAPermissionsProblem()
        {
            var subject = CreateSubject(AnInstanceThatAnswers(HttpStatusCode.Forbidden));

            var result = await subject.ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("insufficient_permissions"));
            }
        }

        [Test]
        public async Task ValidatingATeamAgainstAnInstanceThatCannotBeReached_IsToldTheInstanceIsNotThere()
        {
            var subject = CreateSubject(AnInstanceThatFails(new HttpRequestException("No such host is known.")));

            var result = await subject.ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("connection_failed"));
            }
        }

        // The whole point of AC6, driven through the connector: the flow coach fat-fingers a field
        // name, ServiceNow drops the term and hands back the entire table, and Lighthouse stops
        // rather than rendering the instance's metrics as the team's.
        [Test]
        public async Task ValidatingAQueryThatTheInstanceSilentlyIgnored_StopsRatherThanAcceptingWholeInstanceMetrics()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10, ignoresTheQuery: true);
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("query_matches_whole_table"));
            }
        }

        [Test]
        public async Task ValidatingAQueryThatSelectsOneTeamsWork_Passes()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10, matchedByTheQuery: 2);
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Code, Is.EqualTo("valid"));
            }
        }

        private static ServiceNowWorkTrackingConnector CreateSubject(StubbedInstance instance)
        {
            return new ServiceNowWorkTrackingConnector(
                Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                NoOpAuthStrategyFactory(),
                instance.Handler);
        }

        private static IWorkTrackingAuthStrategyFactory NoOpAuthStrategyFactory()
        {
            var strategy = new Mock<IWorkTrackingAuthStrategy>();
            strategy
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategy.Object);

            return factory.Object;
        }

        private static Team ATeam(string query = TeamsOwnQuery, string table = ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable)
        {
            return new Team
            {
                Name = "Service Desk",
                DataRetrievalValue = query,
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Resolved", "Closed"],
                WorkTrackingSystemConnection = AConnection(table),
            };
        }

        private static WorkTrackingSystemConnection AConnection(string table = ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = InstanceUrl },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Username, Value = "lighthouse.integration" },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Password, Value = "encrypted-secret", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.WorkItemTable, Value = table, IsOptional = true },
            ]);

            return connection;
        }

        // INC0000001 is the record ADR-117 is about: resolved, never closed, and its resolution
        // instant falls on a different day in the instance's own timezone than in universal time.
        // INC0000005 sits in a label the team has not mapped.
        private static List<string> FiveRecordsOfMixedState()
        {
            return
            [
                ARecord("INC0000001", "Resolved", "6", resolvedDisplay: "2026-07-29 17:25:29", resolvedValue: "2026-07-30 00:25:29"),
                ARecord("INC0000002", "Resolved", "6", resolvedDisplay: "2026-07-28 09:00:00", resolvedValue: "2026-07-28 16:00:00"),
                ARecord("INC0000003", "In Progress", "2"),
                ARecord("INC0000004", "New", "1"),
                ARecord("INC0000005", "Awaiting Vendor", "18"),
            ];
        }

        private static string ARecord(
            string number,
            string stateLabel,
            string stateValue,
            string resolvedDisplay = "",
            string resolvedValue = "")
        {
            return $$"""
                {
                  "number": { "display_value": "{{number}}", "value": "{{number}}" },
                  "short_description": { "display_value": "Request {{number}}", "value": "Request {{number}}" },
                  "state": { "display_value": "{{stateLabel}}", "value": "{{stateValue}}" },
                  "sys_created_on": { "display_value": "2026-07-01 00:00:00", "value": "2026-07-01 07:00:00" },
                  "opened_at": { "display_value": "2026-07-01 00:00:00", "value": "2026-07-01 07:00:00" },
                  "resolved_at": { "display_value": "{{resolvedDisplay}}", "value": "{{resolvedValue}}" },
                  "closed_at": { "display_value": "", "value": "" }
                }
                """;
        }

        private static StubbedInstance AnInstanceHolding(
            List<string> records, int pageSize = 100, bool ignoresTheQuery = false, int? matchedByTheQuery = null)
        {
            return StubbedInstance.Holding(records, pageSize, ignoresTheQuery, matchedByTheQuery);
        }

        private static StubbedInstance AnInstanceThatAnswers(HttpStatusCode statusCode)
        {
            return StubbedInstance.Answering(statusCode);
        }

        private static StubbedInstance AnInstanceThatFails(Exception exception)
        {
            return StubbedInstance.Failing(exception);
        }

        // A ServiceNow instance that behaves the way the measured one does: it honours
        // sysparm_offset, caps its own page size regardless of the requested sysparm_limit, and
        // reports the true total in X-Total-Count with a Link header carrying the paging relations.
        private sealed class StubbedInstance
        {
            private StubbedInstance(HttpMessageHandler handler, List<Uri> requests)
            {
                Handler = handler;
                Requests = requests;
            }

            public HttpMessageHandler Handler { get; }

            public List<Uri> Requests { get; }

            public static StubbedInstance Holding(List<string> records, int pageSize, bool ignoresTheQuery, int? matchedByTheQuery)
            {
                var requests = new List<Uri>();

                var handler = HandlerRespondingWith((request) =>
                {
                    var uri = request.RequestUri ?? new Uri(InstanceUrl);
                    requests.Add(uri);

                    var isFiltered = uri.Query.Contains("sysparm_query=", StringComparison.Ordinal)
                        && !uri.Query.Contains("sysparm_query=&", StringComparison.Ordinal);

                    var visible = (isFiltered && !ignoresTheQuery && matchedByTheQuery.HasValue)
                        ? records.Take(matchedByTheQuery.Value).ToList()
                        : records;

                    var offset = NumberFromQuery(uri.Query, "sysparm_offset");
                    var page = visible.Skip(offset).Take(pageSize).ToList();

                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"{{\"result\":[{string.Join(",", page)}]}}", Encoding.UTF8, "application/json"),
                    };

                    response.Headers.TryAddWithoutValidation("X-Total-Count", visible.Count.ToString(CultureInfo.InvariantCulture));
                    response.Headers.TryAddWithoutValidation("Link", LinkHeaderFor(uri, offset, pageSize, visible.Count));

                    return response;
                });

                return new StubbedInstance(handler, requests);
            }

            public static StubbedInstance Answering(HttpStatusCode statusCode)
            {
                var requests = new List<Uri>();

                var handler = HandlerRespondingWith((request) =>
                {
                    requests.Add(request.RequestUri ?? new Uri(InstanceUrl));
                    return new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent("{\"error\":{\"message\":\"denied\"}}", Encoding.UTF8, "application/json"),
                    };
                });

                return new StubbedInstance(handler, requests);
            }

            public static StubbedInstance Failing(Exception exception)
            {
                var requests = new List<Uri>();

                var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
                handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                handler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ThrowsAsync(exception);

                return new StubbedInstance(handler.Object, requests);
            }

            private static HttpMessageHandler HandlerRespondingWith(Func<HttpRequestMessage, HttpResponseMessage> respond)
            {
                var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
                handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                handler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync((HttpRequestMessage request, CancellationToken _) => respond(request));

                return handler.Object;
            }

            private static string LinkHeaderFor(Uri uri, int offset, int pageSize, int total)
            {
                var withoutOffset = uri.GetLeftPart(UriPartial.Path);
                var next = offset + pageSize;
                var links = new List<string> { $"<{withoutOffset}?sysparm_offset=0>;rel=\"first\"" };

                if (next < total)
                {
                    links.Add($"<{withoutOffset}?sysparm_offset={next}>;rel=\"next\"");
                }

                links.Add($"<{withoutOffset}?sysparm_offset={Math.Max(0, total - pageSize)}>;rel=\"last\"");

                return string.Join(",", links);
            }

            private static int NumberFromQuery(string query, string key)
            {
                foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var separator = pair.IndexOf('=', StringComparison.Ordinal);

                    if (separator > 0
                        && pair[..separator].Equals(key, StringComparison.Ordinal)
                        && int.TryParse(pair[(separator + 1)..], CultureInfo.InvariantCulture, out var value))
                    {
                        return value;
                    }
                }

                return 0;
            }
        }
    }
}
