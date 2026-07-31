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
    // Story #5577, US-04 AC1 / AC2 / AC4 + ADR-118 decision 7.
    //
    // Layer 3 (real adapter, stubbed transport). The pure cores are tested next door; this file is
    // about what the connector asks a ServiceNow instance for once it wants history, and what it does
    // with the three answers it can get: spans, a refusal, or nothing that measures state.
    //
    // The stub routes by table, the way the real instance does — incident, metric_definition and
    // metric_instance are three different reads and the connector has to get all three right.
    [TestFixture]
    public class ServiceNowTransitionHistoryTest
    {
        private const string InstanceUrl = "https://dev12345.service-now.com/";
        private const string TeamsOwnQuery = "assignment_group.name=Service Desk^active=true";
        private const string RecordId = "7f10b53a83da4310ad56c670ceaad387";

        // A record the team calls finished, whose closed_at and whose Resolved span name different
        // instants — which is what lets one assertion tell the two sources apart (ADR-117 amended).
        private const string FinishedRecordId = "bbbb222283da4310ad56c670ceaad311";

        private const string StateSpanDefinition = "35f2b283c0a808ae000b7132cd0a4f55";

        // AC1. The whole point of the slice: the connector stops answering from a constant and starts
        // answering from what the instance said.
        [Test]
        public async Task AnInstanceThatMeasuresStateSpans_IsDeclaredToSupplyHistory()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.True);
        }

        // AC4, the runtime downgrade. The rights can be revoked after a connection validated
        // perfectly well, and the sync that follows must fall back rather than fail — WorkItemService
        // only runs its sync-delta derivation when the connector declares history unsupported.
        [Test]
        public async Task AnInstanceThatRefusesTheMetricTables_DowngradesRatherThanFailing()
        {
            var instance = AnInstanceRefusing(HttpStatusCode.Forbidden);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty, "The team's work still syncs. Only the history is missing.");
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False);
            }
        }

        // The second cause, and it must not be reported as the first. An instance where the state
        // metric was disabled answers 200 with nothing matching.
        [Test]
        public async Task AnInstanceMeasuringNoStateSpans_DowngradesRatherThanFailing()
        {
            var instance = AnInstanceThatMeasuresNothing();
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty);
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False);
            }
        }

        // DoD 5 forbids the silent no-op. A team quietly losing time-in-state reads as a team whose
        // work never moves, and the administrator has no way to discover why.
        [Test]
        public async Task DowngradingHistory_SaysSoRatherThanGoingQuiet()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var subject = CreateSubject(AnInstanceRefusing(HttpStatusCode.Forbidden), logger);

            await subject.GetWorkItemsForTeam(ATeam());

            logger.Verify(
                call => call.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        // ADR-118 D2. The definitions have to be resolved before the spans are asked for, or the
        // span read cannot be restricted to the ones that measure state.
        [Test]
        public async Task ReadingHistory_ResolvesTheStateMetricBeforeAskingForSpans()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            var asked = instance.Requests.Select(uri => Uri.UnescapeDataString(uri.AbsoluteUri)).ToList();
            var definitionRead = asked.FindIndex(uri => uri.Contains("/api/now/table/metric_definition", StringComparison.Ordinal));
            var spanRead = asked.FindIndex(uri => uri.Contains("/api/now/table/metric_instance", StringComparison.Ordinal));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitionRead, Is.GreaterThanOrEqualTo(0), "Without this read there is nothing to filter the spans by.");
                Assert.That(spanRead, Is.GreaterThan(definitionRead));
            }
        }

        // SPIKE Q7: ~600ms per call and no rate limiting, so the constraint is wall clock. One call
        // per work item would turn a 500-item sync into five minutes.
        [Test]
        public async Task ReadingHistory_AsksForEveryRecordAtOnceRatherThanOneAtATime()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            var spanReads = instance.Requests
                .Select(uri => Uri.UnescapeDataString(uri.AbsoluteUri))
                .Where(uri => uri.Contains("/api/now/table/metric_instance", StringComparison.Ordinal))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(spanReads, Has.Count.EqualTo(1), "Three records fit in one batch of 200.");
                Assert.That(spanReads[0], Does.Contain("idIN"), "The batch is an IN list of sys_ids, which is what makes one call enough.");
            }
        }

        // AC2 end to end. The pure mapper being right is worth nothing if the connector never hangs
        // the transitions on the work items.
        [Test]
        public async Task WorkSyncedWithHistory_CarriesTheMovesItMade()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000001");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.SyncedTransitions, Is.Not.Empty);
                Assert.That(workItem.SyncedTransitions.Select(transition => transition.ToState), Does.Contain("In Progress"));
            }
        }

        // ADR-118 decision 7, the reason the itil escalation is worth paying for. opened_at is nine
        // days before work began in this fixture, and counting that as work is what ADR-117 accepted
        // only until this slice existed.
        [Test]
        public async Task WhenHistoryIsAvailable_WorkStartedWhenItReachedDoing()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000001");

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 29, 9, 0, 0, DateTimeKind.Utc)),
                "Not opened_at. The span is when someone actually picked the work up.");
        }

        // The other half of decision 7, and the maintainer's ruling: without rights or without a
        // metric, ADR-117's opened_at is still the honest answer and must not disappear.
        [Test]
        public async Task WhenHistoryIsUnavailable_WorkStartedWhenTheRequestArrived()
        {
            var instance = AnInstanceRefusing(HttpStatusCode.Forbidden);
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000001");

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 20, 6, 0, 0, DateTimeKind.Utc)),
                "ADR-117's fallback. Inflated by queue time, and the only thing the record itself supports.");
        }

        // ADR-117 decision 1 as amended 2026-07-31, the counterpart of decision 7. Where the spans
        // exist they say when the work reached Done, and they outrank closed_at — which is what makes
        // a shop that never moves a record past Resolved measurable at all.
        [Test]
        public async Task WhenHistoryIsAvailable_WorkFinishedWhenItReachedDone()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000003");

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 10, 0, 0, DateTimeKind.Utc)),
                "Not closed_at, which this record puts a day later. The span is when the work actually stopped.");
        }

        [Test]
        public async Task WhenHistoryIsUnavailable_WorkFinishedWhenTheRecordSaysItClosed()
        {
            var instance = AnInstanceRefusing(HttpStatusCode.Forbidden);
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000003");

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 31, 15, 0, 0, DateTimeKind.Utc)),
                "ADR-117's fallback. closed_at is the only instant on the record that means the work is over.");
        }

        // A team whose query matched nothing must not ask for the history of every record in the
        // instance — an unfiltered idIN is an unfiltered read.
        [Test]
        public async Task ATeamWithNoWork_AsksForNoHistoryAtAll()
        {
            var instance = AnInstanceHolding([], measuresStateSpans: true);
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(
                instance.Requests.Where(uri => uri.AbsoluteUri.Contains("metric_instance", StringComparison.Ordinal)),
                Is.Empty);
        }

        private static Team ATeam()
        {
            return new Team
            {
                Name = "Service Desk",
                DataRetrievalValue = TeamsOwnQuery,
                // Every ServiceNow team names the kinds of work it handles (#5611); Team's own
                // Jira-shaped default is one this connector never sees.
                WorkItemTypes = ["incident"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Resolved", "Closed"],
                WorkTrackingSystemConnection = AConnection(),
            };
        }

        private static WorkTrackingSystemConnection AConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = InstanceUrl },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.WorkItemTable, Value = "incident", IsOptional = true },
            ]);

            return connection;
        }

        private static ServiceNowWorkTrackingConnector CreateSubject(
            StubbedInstance instance, Mock<ILogger<ServiceNowWorkTrackingConnector>>? logger = null)
        {
            var authStrategy = new Mock<IWorkTrackingAuthStrategy>();
            authStrategy
                .Setup(strategy => strategy.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(authStrategy.Object);

            return new ServiceNowWorkTrackingConnector(
                (logger ?? new Mock<ILogger<ServiceNowWorkTrackingConnector>>()).Object,
                factory.Object,
                instance.Handler);
        }

        private static StubbedInstance AnInstanceThatMeasuresStateSpans()
        {
            return AnInstanceHolding(ThreeRecords(), measuresStateSpans: true);
        }

        private static StubbedInstance AnInstanceThatMeasuresNothing()
        {
            return AnInstanceHolding(ThreeRecords(), measuresStateSpans: false);
        }

        private static StubbedInstance AnInstanceRefusing(HttpStatusCode statusCode)
        {
            return AnInstanceHolding(ThreeRecords(), measuresStateSpans: false, metricStatusCode: statusCode);
        }

        private static StubbedInstance AnInstanceHolding(
            List<string> records, bool measuresStateSpans, HttpStatusCode metricStatusCode = HttpStatusCode.OK)
        {
            return new StubbedInstance(records, measuresStateSpans, metricStatusCode);
        }

        private static List<string> ThreeRecords()
        {
            return
            [
                ARecord("INC0000001", RecordId, "In Progress"),
                ARecord("INC0000002", "aaaa1111", "New"),
                ARecord("INC0000003", FinishedRecordId, "Resolved", closedAt: "2026-07-31 15:00:00"),
            ];
        }

        // opened_at is deliberately nine days before the In Progress span: that gap is the queue time
        // ADR-117 has been counting as work, and the thing this slice removes.
        private static string ARecord(string number, string sysId, string state, string closedAt = "")
        {
            return $$"""
                {
                  "sys_id": { "display_value": "{{sysId}}", "value": "{{sysId}}" },
                  "number": { "display_value": "{{number}}", "value": "{{number}}" },
                  "short_description": { "display_value": "Request {{number}}", "value": "Request {{number}}" },
                  "state": { "display_value": "{{state}}", "value": "2" },
                  "sys_created_on": { "display_value": "2026-07-19 23:00:00", "value": "2026-07-20 06:00:00" },
                  "opened_at": { "display_value": "2026-07-19 23:00:00", "value": "2026-07-20 06:00:00" },
                  "closed_at": { "display_value": "{{closedAt}}", "value": "{{closedAt}}" }
                }
                """;
        }

        // Routes by table the way the instance does. The three reads are genuinely different
        // requests, and a connector that conflated them would still look plausible against a stub
        // that answered everything the same way.
        internal sealed class StubbedInstance
        {
            private readonly List<string> records;
            private readonly bool measuresStateSpans;
            private readonly HttpStatusCode metricStatusCode;

            public StubbedInstance(List<string> records, bool measuresStateSpans, HttpStatusCode metricStatusCode)
            {
                this.records = records;
                this.measuresStateSpans = measuresStateSpans;
                this.metricStatusCode = metricStatusCode;

                Requests = [];

                var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
                handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                handler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync((HttpRequestMessage request, CancellationToken _) => Answer(request));

                Handler = handler.Object;
            }

            public HttpMessageHandler Handler { get; }

            public List<Uri> Requests { get; }

            private HttpResponseMessage Answer(HttpRequestMessage request)
            {
                var uri = request.RequestUri ?? new Uri(InstanceUrl);
                Requests.Add(uri);

                var path = uri.AbsolutePath;

                if (path.Contains("metric_definition", StringComparison.Ordinal)
                    || path.Contains("metric_instance", StringComparison.Ordinal))
                {
                    return MetricAnswer(path);
                }

                return Rows(records);
            }

            private HttpResponseMessage MetricAnswer(string path)
            {
                if (metricStatusCode != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(metricStatusCode)
                    {
                        Content = new StringContent("{\"error\":{\"message\":\"denied\"}}", Encoding.UTF8, "application/json"),
                    };
                }

                if (!measuresStateSpans)
                {
                    return Rows([]);
                }

                return path.Contains("metric_definition", StringComparison.Ordinal)
                    ? Rows([ADefinition()])
                    : Rows(
                    [
                        ASpan(RecordId, "New", "2026-07-20 06:00:00"),
                        ASpan(RecordId, "In Progress", "2026-07-29 09:00:00"),
                        ASpan(FinishedRecordId, "In Progress", "2026-07-29 09:00:00"),
                        ASpan(FinishedRecordId, "Resolved", "2026-07-30 10:00:00"),
                    ]);
            }

            private static string ADefinition()
            {
                return $$"""
                    {
                      "sys_id": { "display_value": "{{StateSpanDefinition}}", "value": "{{StateSpanDefinition}}" },
                      "name": { "display_value": "Incident State Duration", "value": "Incident State Duration" },
                      "type": { "display_value": "Field value duration", "value": "field_value_duration" },
                      "field": { "display_value": "incident_state", "value": "incident_state" },
                      "table": { "display_value": "incident", "value": "incident" }
                    }
                    """;
            }

            private static string ASpan(string record, string label, string start)
            {
                return $$"""
                    {
                      "id": { "display_value": "Incident", "value": "{{record}}" },
                      "definition": { "display_value": "Incident State Duration", "value": "{{StateSpanDefinition}}" },
                      "field": { "display_value": "incident_state", "value": "incident_state" },
                      "value": { "display_value": "{{label}}", "value": "{{label}}" },
                      "field_value": { "display_value": "1", "value": "1" },
                      "start": { "display_value": "{{start}}", "value": "{{start}}" },
                      "end": { "display_value": "", "value": "" }
                    }
                    """;
            }

            private static HttpResponseMessage Rows(List<string> rows)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"result\":[{string.Join(",", rows)}]}}", Encoding.UTF8, "application/json"),
                };

                response.Headers.TryAddWithoutValidation("X-Total-Count", rows.Count.ToString(CultureInfo.InvariantCulture));

                return response;
            }
        }
    }
}
