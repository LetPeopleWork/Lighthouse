using System.Globalization;
using System.Linq.Expressions;
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
    // Story #5611 slice 01, AC-B1 / AC-B2 / AC-B3 / AC-B5 / AC-B6. ADR-123 and ADR-124.
    //
    // A team that handles more than one kind of work, expressed as the connector sees it. Layer 3
    // (real adapter, stubbed transport): sad paths are enumerated one example each, never generated.
    // The record-to-work-item rules live in ServiceNowWorkItemMapperTest; paging and the query
    // verdict live next door in ServiceNowTeamSyncTest. This file is about which kinds of work reach
    // a team, and what the team is told when a kind it named cannot be read.
    //
    // The stub routes by table the way the instance does and honours the class filter, so a
    // connector that does not emit one gets every kind of work back rather than a passing test.
    [TestFixture]
    public class ServiceNowRecordClassTest
    {
        private const string InstanceUrl = "https://dev12345.service-now.com/";
        private const string TeamsOwnQuery = "assignment_group.name=Service Desk^active=true";

        private const string TheWholeHierarchy = "task";
        private const string Incidents = "incident";
        private const string Changes = "change_request";
        private const string Problems = "problem";

        // What a flow coach typing the label "Change Request" instead of the system name reaches.
        private const string NoSuchKindOfWork = "not_a_real_class";

        private static readonly string[] IncidentsAndChanges = [Incidents, Changes];
        private static readonly string[] IncidentsChangesAndProblems = [Incidents, Changes, Problems];
        private static readonly string[] BothKindsOfWork = ["INC0000001", "CHG0000001"];

        // The walking skeleton. A flow coach whose team handles incidents and changes points one
        // Lighthouse team at both and sees both, each labelled with the kind of work it actually is —
        // and sees nothing of the third kind sitting in the same hierarchy.
        [Test]
        public async Task ATeamThatHandlesIncidentsAndChanges_SeesBothKindsOfWorkAsOneTeam()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            var workItems = (await subject.GetWorkItemsForTeam(
                ATeamWorkingOn(IncidentsAndChanges, rootedAt: TheWholeHierarchy))).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems.Select(item => item.ReferenceId), Is.EquivalentTo(BothKindsOfWork),
                    "One team, both kinds of work — and the problem record is a kind this team never named.");
                Assert.That(workItems.Select(item => item.Type), Is.EquivalentTo(IncidentsAndChanges),
                    "Each row is labelled with the kind of work it is, not with the hierarchy the team is rooted at.");
            }
        }

        // AC-B1. One read, one paging walk, one repeat guard — the whole reason the model is a class
        // filter rather than a read per kind of work (D2).
        [Test]
        public async Task ATeamThatHandlesSeveralKindsOfWork_AsksForThemInOneRead()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeamWorkingOn(IncidentsAndChanges, rootedAt: TheWholeHierarchy));

            var workReads = QueriesAskedOf(instance, TheWholeHierarchy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workReads, Has.Count.EqualTo(1),
                    "Several kinds of work is still one read. A read per kind multiplies the paging walk and the repeat guard by the number of kinds.");
                Assert.That(workReads[0], Does.Contain("sys_class_nameINincident,change_request"),
                    "ADR-123 decision 2: one IN condition, never a chain of ^OR, against the URL budget.");
                Assert.That(
                    workReads[0].IndexOf("sys_class_name", StringComparison.Ordinal),
                    Is.LessThan(workReads[0].IndexOf(TeamsOwnQuery, StringComparison.Ordinal)),
                    "The clause is prepended, ahead of the team's own query and of the ORDERBY the connector appends.");
            }
        }

        // ADR-123 decision 2's other half. A one-element IN was never measured and the equals form
        // was, so a team on a single kind of work asks the shape that is on record.
        [Test]
        public async Task ATeamThatHandlesOneKindOfWork_AsksForItByName()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeamWorkingOn([Incidents], rootedAt: TheWholeHierarchy));

            var workReads = QueriesAskedOf(instance, TheWholeHierarchy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workReads[0], Does.Contain("sys_class_name=incident"));
                Assert.That(workReads[0], Does.Not.Contain("sys_class_nameIN"));
            }
        }

        // AC-B5. Every shipped team is byte-identical on the wire — same URL, same query. This is the
        // claim that makes "this story does not make the shipped configuration harder" checkable
        // rather than hoped for.
        [Test]
        public async Task AnIncidentTeamThatNamedNoKindsOfWork_AsksExactlyWhatItAskedBefore()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeamWorkingOn([], rootedAt: Incidents));

            var workReads = QueriesAskedOf(instance, Incidents);

            Assert.That(workReads[0], Does.Not.Contain("sys_class_name"),
                "A team rooted at a single kind of work reads exactly the way it did before this story existed.");
        }

        // AC-B3. The epic's AC1 rule ("a team that has not said which work is theirs reads nothing
        // rather than everything") applied to the kind-of-work dimension. Unfiltered, the same team
        // reads the whole instance's work: 579 records of 13 kinds where it wanted 159 of 2.
        [Test]
        public async Task ATeamOnTheWholeHierarchyThatNamedNoKindsOfWork_ReadsNothingRatherThanEverything()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance, logger.Object);

            var workItems = await subject.GetWorkItemsForTeam(ATeamWorkingOn([], rootedAt: TheWholeHierarchy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Empty);
                Assert.That(instance.Requests, Is.Empty,
                    "Asking the hierarchy without naming a kind of work returns every kind in it.");
            }

            logger.Verify(AWarningContaining(TheWholeHierarchy), Times.Once,
                "DoD 5 forbids the silent no-op: reading nothing has to say why, and name the table it refused to read.");
        }

        // AC-B3's second home (ADR-123 decision 4). isWorkItemTypesRequired is a hint to the web UI,
        // and PUT /api/teams/{id} also serves the CLI and the MCP server. A gate that lives only in
        // the schema flag is a gate the API does not have.
        [Test]
        public async Task SavingATeamOnTheWholeHierarchyThatNamedNoKindsOfWork_IsAskedWhichKindsWithoutContactingTheInstance()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(ATeamWorkingOn([], rootedAt: TheWholeHierarchy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("missing_work_item_types"));
                Assert.That(result.FieldName, Is.EqualTo("WorkItemTypes"),
                    "The settings screen routes the message to the field the flow coach has to fix.");
                Assert.That(instance.Requests, Is.Empty, "Pre-flight. Nothing to ask the instance yet.");
            }
        }

        // AC-B5. The shipped configuration keeps saving without the field.
        [Test]
        public async Task SavingAnIncidentTeamThatNamedNoKindsOfWork_IsStillAccepted()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(ATeamWorkingOn([], rootedAt: Incidents));

            Assert.That(result.Code, Is.EqualTo("valid"));
        }

        // AC-B6, ADR-124 rung 1. The most likely mistake there is: the flow coach reads
        // "Change Request" on their own screen and has to type change_request. A wrong name narrows
        // the read to nothing in silence, so it has to be caught at the moment it is typed.
        [Test]
        public async Task SavingATeamThatNamesAKindOfWorkTheInstanceDoesNotHave_IsToldWhichNameIsWrong()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork())
                .Where(NoSuchKindOfWork, HttpStatusCode.BadRequest, holds: 0, visible: 0);
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(
                ATeamWorkingOn([Incidents, NoSuchKindOfWork], rootedAt: TheWholeHierarchy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("unknown_table"));
                Assert.That(result.Message, Does.Contain(NoSuchKindOfWork),
                    "The name that is wrong has to be in the message, or there is nothing to correct.");
                Assert.That(result.FieldName, Is.EqualTo("WorkItemTypes"));
            }
        }

        // AC-B6, ADR-124 rung 2. Retained because an instance with class-level ACLs configured
        // differently can reach it, even though no ITSM class produced a 403 at any privilege level
        // on the instance this was measured against.
        [Test]
        public async Task SavingATeamThatNamesAKindOfWorkTheInstanceRefuses_IsToldItIsAPermissionsProblem()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork())
                .Where(Problems, HttpStatusCode.Forbidden, holds: 0, visible: 0);
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(
                ATeamWorkingOn([Incidents, Problems], rootedAt: TheWholeHierarchy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("insufficient_permissions"));
                Assert.That(result.Message, Does.Contain(Problems));
            }
        }

        // AC-B6, ADR-124 rung 4 — the rung the whole feature rests on. An account that may read
        // incidents but not problems gets a 200 with the problem rows simply absent: no error, no
        // header, no partial-result marker. X-Total-Count is ACL-blind, so a count above zero with an
        // empty body is the one signal there is that a kind of work is being hidden rather than
        // being empty.
        [Test]
        public async Task SavingATeamThatNamesAKindOfWorkTheAccountCannotSee_IsToldWhichKindIsHidden()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork())
                .Where(Problems, HttpStatusCode.OK, holds: 24, visible: 0);
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(
                ATeamWorkingOn([Incidents, Problems], rootedAt: TheWholeHierarchy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Message, Does.Contain(Problems),
                    "The kind of work that cannot be read has to be named, or the team is quietly two thirds of itself.");
                Assert.That(result.FieldName, Is.EqualTo("WorkItemTypes"));
                Assert.That(result.Code, Is.EqualTo("class_records_not_visible"),
                    "T-1, settled by the maintainer: it parallels no_records_visible at connection scope and states what was seen without asserting a cause the platform cannot supply.");
            }
        }

        // OQ-8, settled by the maintainer: a kind of work with nothing in it is a legitimate
        // configuration, and refusing the save would block a team on a quiet quarter. The probe still
        // has to happen — otherwise this passes for the reason that nothing was checked.
        [Test]
        public async Task SavingATeamThatNamesAKindOfWorkWithNothingInItYet_IsAccepted()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork())
                .Where(Changes, HttpStatusCode.OK, holds: 0, visible: 0);
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(
                ATeamWorkingOn(IncidentsAndChanges, rootedAt: TheWholeHierarchy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("valid"));
                Assert.That(ProbesOf(instance, Changes), Has.Count.EqualTo(1),
                    "An empty kind of work passes because it was asked about and answered, not because nobody asked.");
            }
        }

        // S2 and OQ-5: one cheap probe per named kind of work, at the one moment a human is already
        // waiting on a Save, and never on a refresh. Serial and uncapped, matching every other read
        // in this connector.
        [Test]
        public async Task SavingATeamThatNamesThreeKindsOfWork_AsksTheInstanceAboutEachOfThemOnce()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            await subject.ValidateTeamSettings(ATeamWorkingOn(IncidentsChangesAndProblems, rootedAt: TheWholeHierarchy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ProbesOf(instance, Incidents), Has.Count.EqualTo(1));
                Assert.That(ProbesOf(instance, Changes), Has.Count.EqualTo(1));
                Assert.That(ProbesOf(instance, Problems), Has.Count.EqualTo(1));
                Assert.That(instance.Requests, Has.Count.EqualTo(5),
                    "Three kinds of work plus the two counts the widening detector already costs. Nothing fans out.");
            }
        }

        // S1 and ADR-124 decision 3. The widening detector keeps meaning what its message says: how
        // much of YOUR kind of work did this query select, rather than how much of the instance.
        // Left alone, a hierarchy-rooted team compares a correct answer against the whole hierarchy.
        [Test]
        public async Task SavingATeamThatHandlesSeveralKindsOfWork_MeasuresItsQueryAgainstItsOwnKindsOfWork()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            await subject.ValidateTeamSettings(ATeamWorkingOn(IncidentsAndChanges, rootedAt: TheWholeHierarchy));

            var counts = QueriesAskedOf(instance, TheWholeHierarchy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(counts, Has.Count.EqualTo(2));
                Assert.That(counts, Is.All.Contain("sys_class_nameINincident,change_request"),
                    "Both sides of the comparison count the same kinds of work.");
                Assert.That(counts, Has.Exactly(1).Contains(TeamsOwnQuery),
                    "One side carries the team's query and the other does not. That difference IS the detection.");
            }
        }

        // S4. metric_definition rows attach to concrete kinds of work and never to the base table, so
        // a team on the whole hierarchy finds zero definitions and silently loses every started date
        // and state span slice 04 shipped — via the very feature that recommends rooting there.
        [Test]
        public async Task ATeamThatHandlesSeveralKindsOfWork_LooksForStateHistoryOnEachOfThoseKinds()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeamWorkingOn(IncidentsAndChanges, rootedAt: TheWholeHierarchy));

            Assert.That(QueriesAskedOf(instance, "metric_definition"),
                Has.Some.Contains("tableINincident,change_request"),
                "Definitions attach to the kinds of work, never to the hierarchy they sit in.");
        }

        // AC-B5's history half. A shipped team looks for state history exactly where it did before.
        [Test]
        public async Task AnIncidentTeamThatNamedNoKindsOfWork_LooksForStateHistoryExactlyWhereItDidBefore()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeamWorkingOn([], rootedAt: Incidents));

            Assert.That(QueriesAskedOf(instance, "metric_definition"),
                Has.Some.Contains("table=incident"));
        }

        // D-D10. Asked of the whole hierarchy, "can this instance measure how long work sat in a
        // state" has no answer, and the answer it gives today is actively wrong: it tells the
        // administrator to activate a definition on the state field of task, advice that cannot be
        // followed and that contradicts what their teams will get. One false statement not made.
        [Test]
        [Ignore("DISTILL scaffold for #5611 slice 01 — un-skip in DELIVER (ADR-025).")]
        public async Task ValidatingAConnectionRootedAtTheWholeHierarchy_SaysStateHistoryIsDecidedPerTeam()
        {
            var instance = AnInstanceHolding(ThreeKindsOfWork());
            var subject = CreateSubject(instance);

            var result = await subject.ValidateConnection(AConnection(TheWholeHierarchy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.AdvisoryCode, Is.EqualTo("history_determined_per_team"));
                Assert.That(QueriesAskedOf(instance, "metric_definition"), Is.Empty,
                    "One request saved, because there is nothing meaningful to read.");
            }
        }

        private static List<string> QueriesAskedOf(StubbedInstance instance, string table)
        {
            return instance.Requests
                .Where(uri => uri.AbsolutePath.EndsWith($"/{table}", StringComparison.Ordinal))
                .Select(uri => Uri.UnescapeDataString(uri.Query))
                .ToList();
        }

        private static List<Uri> ProbesOf(StubbedInstance instance, string table)
        {
            return instance.Requests
                .Where(uri => uri.AbsolutePath.EndsWith($"/{table}", StringComparison.Ordinal))
                .ToList();
        }

        private static Expression<Action<ILogger<ServiceNowWorkTrackingConnector>>> AWarningContaining(string text)
        {
            return log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => $"{state}".Contains(text, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>());
        }

        private static ServiceNowWorkTrackingConnector CreateSubject(
            StubbedInstance instance, ILogger<ServiceNowWorkTrackingConnector>? logger = null)
        {
            var strategy = new Mock<IWorkTrackingAuthStrategy>();
            strategy
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategy.Object);

            return new ServiceNowWorkTrackingConnector(
                logger ?? Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                factory.Object,
                instance.Handler);
        }

        private static Team ATeamWorkingOn(string[] kindsOfWork, string rootedAt)
        {
            return new Team
            {
                Name = "Service Desk",
                DataRetrievalValue = TeamsOwnQuery,
                WorkItemTypes = [.. kindsOfWork],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Resolved", "Closed"],
                WorkTrackingSystemConnection = AConnection(rootedAt),
            };
        }

        private static WorkTrackingSystemConnection AConnection(string table)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = InstanceUrl },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.WorkItemTable, Value = table, IsOptional = true },
            ]);

            return connection;
        }

        // One record of each kind that is this team's, all in states the team maps, so "which kinds
        // came back" is never confounded by the state filter. Plus one incident belonging to another
        // team, so the widening detector has something to compare against and a correct read is
        // strictly narrower than the whole table.
        private static List<Record> ThreeKindsOfWork()
        {
            return
            [
                new Record("INC0000001", Incidents, IsTheTeams: true),
                new Record("CHG0000001", Changes, IsTheTeams: true),
                new Record("PRB0000001", Problems, IsTheTeams: true),
                new Record("INC0000099", Incidents, IsTheTeams: false),
            ];
        }

        private static StubbedInstance AnInstanceHolding(List<Record> records)
        {
            return new StubbedInstance(records);
        }

        internal sealed record Record(string Number, string RecordClass, bool IsTheTeams);

        // What one named kind of work answers when it is probed on its own table (ADR-124 decision 2).
        // Holds is X-Total-Count, which the instance reports without consulting the ACLs; Visible is
        // what the account actually gets back. The gap between the two is the whole mechanism.
        internal sealed record ClassAnswer(HttpStatusCode Status, int Holds, int Visible);

        // Routes by table the way the instance does, and honours the class filter. A connector that
        // emits no filter gets every kind of work back rather than a passing test.
        internal sealed class StubbedInstance
        {
            private readonly List<Record> records;
            private readonly Dictionary<string, ClassAnswer> classAnswers = [];

            public StubbedInstance(List<Record> records)
            {
                this.records = records;

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

            public StubbedInstance Where(string recordClass, HttpStatusCode status, int holds, int visible)
            {
                classAnswers[recordClass] = new ClassAnswer(status, holds, visible);

                return this;
            }

            private HttpResponseMessage Answer(HttpRequestMessage request)
            {
                var uri = request.RequestUri ?? new Uri(InstanceUrl);
                Requests.Add(uri);

                var table = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];

                if (table is "metric_definition" or "metric_instance")
                {
                    return Rows([], 0);
                }

                if (classAnswers.TryGetValue(table, out var answer))
                {
                    return AnswerFor(answer);
                }

                var visible = SelectedBy(Uri.UnescapeDataString(uri.Query), table);

                return Rows([.. visible.Select(AsJson)], visible.Count);
            }

            private static HttpResponseMessage AnswerFor(ClassAnswer answer)
            {
                if (answer.Status != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(answer.Status)
                    {
                        Content = new StringContent("{\"error\":{\"message\":\"denied\"}}", Encoding.UTF8, "application/json"),
                    };
                }

                var rows = Enumerable.Range(0, answer.Visible)
                    .Select(index => AsJson(new Record($"ROW{index.ToString("D7", CultureInfo.InvariantCulture)}", TheWholeHierarchy, IsTheTeams: true)))
                    .ToList();

                return Rows(rows, answer.Holds);
            }

            // The instance honours sys_class_name whether it was asked with IN or with =, and it
            // honours the team's own query. A read that names no kind of work gets every kind that
            // lives under the table it addressed — which is the number D3 exists to prevent.
            private List<Record> SelectedBy(string query, string table)
            {
                var named = NamedIn(query);

                var underTheTable = table == TheWholeHierarchy
                    ? records
                    : records.Where(record => record.RecordClass == table).ToList();

                var ofTheNamedKinds = named.Count < 1
                    ? underTheTable
                    : underTheTable.Where(record => named.Contains(record.RecordClass)).ToList();

                return query.Contains(TeamsOwnQuery, StringComparison.Ordinal)
                    ? ofTheNamedKinds.Where(record => record.IsTheTeams).ToList()
                    : [.. ofTheNamedKinds];
            }

            private static List<string> NamedIn(string query)
            {
                // The encoded query is one parameter among several, and the clause is prepended
                // inside it — so the first condition carries "…&sysparm_query=" ahead of it.
                foreach (var condition in EncodedQueryIn(query).Split('^', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (condition.StartsWith("sys_class_nameIN", StringComparison.Ordinal))
                    {
                        return [.. condition["sys_class_nameIN".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries)];
                    }

                    if (condition.StartsWith("sys_class_name=", StringComparison.Ordinal))
                    {
                        return [condition["sys_class_name=".Length..]];
                    }
                }

                return [];
            }

            private static string EncodedQueryIn(string query)
            {
                const string parameter = "sysparm_query=";

                var start = query.IndexOf(parameter, StringComparison.Ordinal);

                return start < 0 ? string.Empty : query[(start + parameter.Length)..];
            }

            private static HttpResponseMessage Rows(List<string> rows, int holds)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"result\":[{string.Join(",", rows)}]}}", Encoding.UTF8, "application/json"),
                };

                response.Headers.TryAddWithoutValidation("X-Total-Count", holds.ToString(CultureInfo.InvariantCulture));

                return response;
            }

            // sys_class_name rides in the connector's existing sysparm_display_value=all read, which
            // is what makes the item's own kind of work free to read (ADR-123 decision 8).
            private static string AsJson(Record record)
            {
                return $$"""
                    {
                      "sys_id": { "display_value": "{{record.Number}}", "value": "{{record.Number}}" },
                      "sys_class_name": { "display_value": "Readable {{record.RecordClass}}", "value": "{{record.RecordClass}}" },
                      "number": { "display_value": "{{record.Number}}", "value": "{{record.Number}}" },
                      "short_description": { "display_value": "Request {{record.Number}}", "value": "Request {{record.Number}}" },
                      "state": { "display_value": "In Progress", "value": "2" },
                      "sys_created_on": { "display_value": "2026-07-01 00:00:00", "value": "2026-07-01 07:00:00" },
                      "opened_at": { "display_value": "2026-07-01 00:00:00", "value": "2026-07-01 07:00:00" },
                      "resolved_at": { "display_value": "", "value": "" },
                      "closed_at": { "display_value": "", "value": "" }
                    }
                    """;
            }
        }
    }
}
