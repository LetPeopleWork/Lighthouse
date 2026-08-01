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
    // Story #5610 slice 02, AC-B1 / AC-B2 / AC-B3 / AC-B4. ADR-125 and ADR-126.
    //
    // A ServiceNow shop's team boundary usually already exists as a Visual Task Board — a table plus
    // a filter — and this file is what happens when an administrator picks one. Layer 3 (real
    // adapter, stubbed transport): sad paths are enumerated one example each, never generated.
    //
    // The stub honours the board scoping the way the instance does, so a connector that asks for
    // every board gets every board back rather than a passing test; and it reports the instance's own
    // board count without consulting who is asking, which is the lie the SPIKE measured on 2026-08-01
    // (header 2, body 0) and which the list must never be counted from.
    [TestFixture]
    public class ServiceNowBoardPickerTest
    {
        private const string InstanceUrl = "https://dev12345.service-now.com/";

        private const string TheBoardTable = "vtb_board";
        private const string TheWholeHierarchy = "task";

        private const string Incidents = "incident";

        // Measured on the PDI: the column form, which selects 38 of 105 incidents.
        private const string TheBoardsFilter = "correlation_id=LIGHTHOUSE_DEMO";

        // The label form ServiceNow's own screen displays, and the one that selects all 105 of them.
        private const string TheFilterAsItReadsOnScreen = "Correlation ID = LIGHTHOUSE_DEMO^ORDERBY";

        // A real, populated table that is not work: nothing of that kind sits under the hierarchy.
        private const string NotAKindOfWork = "cmdb_ci";

        private const string TheIncidentBoardId = "b1";
        private const string TheChangeBoardId = "b2";

        private static readonly string[] TheBoardsThisConnectionCanUse = ["Incidents Kanban", "Change Requests by State"];
        private static readonly string[] TheKindOfWorkTheBoardHolds = [Incidents];

        // DD-1 / ADR-125 decision 1. ServiceNow joins the board port three other connectors already
        // use: no new endpoint, no new dialog, no contract change. The claim it reverses is written
        // into IServiceNowWorkTrackingConnector's own xmldoc, so it is worth stating out loud.
        [Test]
        public void AServiceNowConnection_CanBeAskedForTheBoardsItAlreadyMaintains()
        {
            var connector = CreateSubject(AnInstanceWith(TheIncidentBoard()));

            Assert.That(connector, Is.InstanceOf<IBoardInformationProvider>(),
                "A ServiceNow instance has Visual Task Boards, so the connector belongs on the board port the wizard already serves three connectors from.");
        }

        // AC-B1. The administrator sees the boards this connection can actually turn into a team.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public async Task AnAdministratorOpeningThePicker_SeesTheBoardsThisConnectionCanTurnIntoATeam()
        {
            var instance = AnInstanceWith(TheIncidentBoard(), TheChangeBoard());

            var boards = (await ABoardPickerFor(instance).GetBoards(AConnection())).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(boards.Select(board => board.Name), Is.EquivalentTo(TheBoardsThisConnectionCanUse));
                Assert.That(boards.Select(board => board.Id), Contains.Item(TheIncidentBoardId));
            }
        }

        // AC-B4 / D14 / ADR-125 decision 3. A board with no table and no filter is a freeform board:
        // its cards are placed by hand and no query describes them. A board with a table but no
        // filter would pre-fill an empty query, which Save then blocks. Neither is worth rendering
        // and refusing, so both are excluded where the instance can do it — in the read.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public async Task ABoardThatCannotBecomeAQuery_NeverReachesTheAdministrator()
        {
            var instance = AnInstanceWith(
                TheIncidentBoard(),
                AFreeformBoard(),
                ABoardWithNoFilter(),
                ABoardNobodyUsesAnyMore());

            var boards = (await ABoardPickerFor(instance).GetBoards(AConnection())).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(boards.Select(board => board.Id), Is.EqualTo(new List<string> { TheIncidentBoardId }),
                    "Only a board that carries both a table and a filter can become a team.");
                Assert.That(TheBoardListReadOf(instance), Does.Contain("tableISNOTEMPTY").And.Contain("filterISNOTEMPTY").And.Contain("active=true"),
                    "The instance can exclude them itself; asking for every board and filtering afterwards reads boards the account has no business seeing.");
            }
        }

        // AC-B2 / DD-2. The board's filter is a verbatim encoded query in column form, so it becomes
        // the team's query unchanged — no translation, no parsing.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public async Task PickingABoard_HandsTheTeamTheBoardsOwnFilterAsItsQuery()
        {
            var instance = AnInstanceWith(TheIncidentBoard());

            var preFill = await ABoardPickerFor(instance).GetBoardInformation(AConnection(), TheIncidentBoardId);

            Assert.That(preFill.DataRetrievalValue, Is.EqualTo(TheBoardsFilter));
        }

        // AC-B2 / D6. The board's table is the kind of work the team handles, which is the field
        // #5611 made every ServiceNow team fill in.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public async Task PickingABoard_HandsTheTeamTheBoardsTableAsTheKindOfWorkItHandles()
        {
            var instance = AnInstanceWith(TheIncidentBoard());

            var preFill = await ABoardPickerFor(instance).GetBoardInformation(AConnection(), TheIncidentBoardId);

            Assert.That(preFill.WorkItemTypes, Is.EqualTo(TheKindOfWorkTheBoardHolds));
        }

        // DD-2 / ADR-125 decision 2. The board holds its filter twice: once in column form and once
        // as the label form ServiceNow's own screen displays. The label form is the legible one, and
        // running it selects the WHOLE table — 105 of 105 incidents, 118 of 118 change requests,
        // measured 2026-08-01. That is precisely the widening Save exists to block, so the readable
        // form is never read, never carried and never shown.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public async Task PickingABoard_NeverHandsOverTheFilterAsItReadsOnTheServiceNowScreen()
        {
            var instance = AnInstanceWith(TheIncidentBoard());

            var preFill = await ABoardPickerFor(instance).GetBoardInformation(AConnection(), TheIncidentBoardId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(preFill.DataRetrievalValue, Is.EqualTo(TheBoardsFilter));
                Assert.That(preFill.DataRetrievalValue, Does.Not.Contain("Correlation ID"),
                    "The legible form of the filter matches every record in the table. A team pre-filled with it forecasts the whole instance.");
            }
        }

        // ADR-125 decision 3. The single-board read re-applies the same scoping rather than trusting
        // the list it served a moment ago: a board that lost its filter in between is refused, not
        // handed over as an empty query.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public void PickingABoardThatNoLongerQualifies_IsRefusedRatherThanHandedOverAsAnEmptyQuery()
        {
            var instance = AnInstanceWith(AFreeformBoard());

            var refusal = Assert.ThrowsAsync<ServiceNowReadException>(
                () => ABoardPickerFor(instance).GetBoardInformation(AConnection(), TheIncidentBoardId));

            Assert.That(refusal, Is.Not.Null);
        }

        // AC-B4 / DD-4, and OC-5's answer. A board can be built on anything — one on cmdb_ci was
        // created on the PDI without complaint — and a team pre-filled from it syncs nothing at all.
        // The class ladder ADR-124 already shipped names that case, in words already written.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public void PickingABoardWhoseWorkIsNotAKindOfWork_IsRefusedByName()
        {
            var instance = AnInstanceWith(ABoardOnSomethingThatIsNotWork())
                .WhereTheHierarchyHoldsNothingOf(NotAKindOfWork)
                .WhereTheTableItself(NotAKindOfWork, HttpStatusCode.OK, holds: 2784, visible: 1);

            var refusal = Assert.ThrowsAsync<ServiceNowReadException>(
                () => ABoardPickerFor(instance).GetBoardInformation(AConnection(), TheChangeBoardId));

            Assert.That(refusal?.Code, Is.EqualTo("class_is_not_a_kind_of_work"));
        }

        // AC-B3 / ADR-126 decision 1. Every failure the SPIKE found used to arrive at the wizard as
        // the same "Failed to load boards. Please try again." — advice that fixes none of them. A
        // refusal keeps the name the backend already gave it, and names the table it was refused on.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public void AnAccountThatMayNotReadBoards_IsToldSoRatherThanShownAnEmptyPicker()
        {
            var instance = AnInstanceWith(TheIncidentBoard()).WhereTheBoardTableAnswers(HttpStatusCode.Forbidden);

            var refusal = Assert.ThrowsAsync<ServiceNowReadException>(
                () => ABoardPickerFor(instance).GetBoards(AConnection()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.Code, Is.EqualTo("insufficient_permissions"));
                Assert.That(refusal?.Message, Does.Contain(TheBoardTable),
                    "Naming the table is what turns 'try again' into something an administrator can grant.");
            }
        }

        // AC-B3 / ADR-126 decision 3, the first rung. A rejected credential is a credential problem
        // wherever it is met.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public void ACredentialTheInstanceRejects_IsToldSoWhenThePickerOpens()
        {
            var instance = AnInstanceWith(TheIncidentBoard()).WhereTheBoardTableAnswers(HttpStatusCode.Unauthorized);

            var refusal = Assert.ThrowsAsync<ServiceNowReadException>(
                () => ABoardPickerFor(instance).GetBoards(AConnection()));

            Assert.That(refusal?.Code, Is.EqualTo("authentication_failed"));
        }

        // DD-7 / ADR-126 decision 3. Boards are shared, not roled: the list is scoped to the account
        // this connection signs in with, and an account nobody has shared a board with reads zero
        // rows. The connection ladder would call that no_records_visible and refuse the whole picker
        // for a customer whose only mistake is not having shared a board yet — an action they can
        // take, reported as a fault they cannot. It is an empty list.
        //
        // The instance still reports two boards in its own count while returning none of them, so a
        // list counted from the header would offer an administrator boards that are not there.
        [Test]
        [Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]
        public async Task AnAccountThatSharesNoBoard_IsOfferedAnEmptyListRatherThanToldTheConnectionIsBroken()
        {
            var instance = AnInstanceWith(TheIncidentBoard(), TheChangeBoard()).WhereTheAccountIsAMemberOfNoBoard();

            var boards = await ABoardPickerFor(instance).GetBoards(AConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheBoardListReadOf(instance), Is.Not.Empty, "The instance was asked, and answered with nothing it was willing to show this account.");
                Assert.That(boards, Is.Empty);
            }
        }

        // AC-B5. Typing a query by hand stays the primary path and stays exactly as it was: saving a
        // team never asks the instance about boards. Stated as an absence claim, so it is a pin
        // rather than a red test — it exists to fail the day the picker leaks onto the Save path and
        // costs every ServiceNow team a request on a path where a human is waiting.
        [Test]
        public async Task ATeamWhoseQueryWasTypedByHand_IsSavedWithoutTheInstanceBeingAskedAboutBoards()
        {
            var instance = AnInstanceWith(TheIncidentBoard());

            await CreateSubject(instance).ValidateTeamSettings(ATeamThatTypedItsOwnQuery());

            Assert.That(TheBoardListReadOf(instance), Is.Empty,
                "The board picker is a wizard the administrator opens, not a step in saving a team.");
        }

        private static Team ATeamThatTypedItsOwnQuery()
        {
            return new Team
            {
                Name = "Service Desk",
                DataRetrievalValue = "assignment_group.name=Service Desk^active=true",
                WorkItemTypes = [.. TheKindOfWorkTheBoardHolds],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Resolved", "Closed"],
                WorkTrackingSystemConnection = AConnection(),
            };
        }

        private static IBoardInformationProvider ABoardPickerFor(StubbedInstance instance)
        {
            return CreateSubject(instance);
        }

        private static string TheBoardListReadOf(StubbedInstance instance)
        {
            var read = instance.Requests.FirstOrDefault(uri => uri.AbsolutePath.EndsWith(TheBoardTable, StringComparison.Ordinal));

            return read is null ? string.Empty : Uri.UnescapeDataString(read.Query);
        }

        private static ServiceNowWorkTrackingConnector CreateSubject(StubbedInstance instance)
        {
            var strategy = new Mock<IWorkTrackingAuthStrategy>();
            strategy
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategy.Object);

            return new ServiceNowWorkTrackingConnector(
                Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                factory.Object,
                instance.Handler);
        }

        private static WorkTrackingSystemConnection AConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.Add(
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = InstanceUrl });

            return connection;
        }

        private static BoardRow TheIncidentBoard()
        {
            return new BoardRow(TheIncidentBoardId, "Incidents Kanban", Incidents, TheBoardsFilter, TheFilterAsItReadsOnScreen);
        }

        private static BoardRow TheChangeBoard()
        {
            return new BoardRow(TheChangeBoardId, "Change Requests by State", "change_request", "state!=3", "State is not Closed");
        }

        // Hand-placed cards, no table and no filter — nothing a query can describe.
        private static BoardRow AFreeformBoard()
        {
            return new BoardRow("b3", "Whiteboard", string.Empty, string.Empty, string.Empty);
        }

        // A real configuration ("all incidents") whose pre-fill would be an empty query.
        private static BoardRow ABoardWithNoFilter()
        {
            return new BoardRow("b4", "Everything", Incidents, string.Empty, string.Empty);
        }

        private static BoardRow ABoardNobodyUsesAnyMore()
        {
            return new BoardRow("b5", "Last Year's Board", Incidents, "state=1", "State is New", IsActive: false);
        }

        private static BoardRow ABoardOnSomethingThatIsNotWork()
        {
            return new BoardRow(TheChangeBoardId, "Server Estate", NotAKindOfWork, "operational_status=1", "Operational status is Operational");
        }

        private static StubbedInstance AnInstanceWith(params BoardRow[] boards)
        {
            return new StubbedInstance([.. boards]);
        }

        internal sealed record BoardRow(string Id, string Name, string Table, string Filter, string ReadableFilter, bool IsActive = true);

        internal sealed record TableAnswer(HttpStatusCode Status, int Holds, int Visible);

        // Routes by table the way the instance does, honours the board scoping, and reports its own
        // board count without consulting who is asking.
        internal sealed class StubbedInstance
        {
            private readonly List<BoardRow> boards;
            private readonly Dictionary<string, TableAnswer> tableAnswers = [];
            private readonly HashSet<string> hierarchyHoldsNothingOf = new(StringComparer.Ordinal);

            private HttpStatusCode boardTableStatus = HttpStatusCode.OK;
            private bool sharedWithThisAccount = true;

            public StubbedInstance(List<BoardRow> boards)
            {
                this.boards = boards;

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

            public StubbedInstance WhereTheBoardTableAnswers(HttpStatusCode status)
            {
                boardTableStatus = status;

                return this;
            }

            /// <summary>Nobody has shared a board with the account this connection signs in with.</summary>
            public StubbedInstance WhereTheAccountIsAMemberOfNoBoard()
            {
                sharedWithThisAccount = false;

                return this;
            }

            /// <summary>The work hierarchy reports no records of a named kind — the first probe's zero.</summary>
            public StubbedInstance WhereTheHierarchyHoldsNothingOf(string recordClass)
            {
                hierarchyHoldsNothingOf.Add(recordClass);

                return this;
            }

            /// <summary>What a named table answers when it is addressed directly — the second probe.</summary>
            public StubbedInstance WhereTheTableItself(string table, HttpStatusCode status, int holds, int visible)
            {
                tableAnswers[table] = new TableAnswer(status, holds, visible);

                return this;
            }

            private HttpResponseMessage Answer(HttpRequestMessage request)
            {
                var uri = request.RequestUri ?? new Uri(InstanceUrl);
                Requests.Add(uri);

                var table = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
                var query = Uri.UnescapeDataString(uri.Query);

                if (table == TheBoardTable)
                {
                    return AnswerAboutBoards(query);
                }

                if (tableAnswers.TryGetValue(table, out var answer))
                {
                    return AnswerWith(answer);
                }

                if (table == TheWholeHierarchy && NamedIn(query) is { } probed && hierarchyHoldsNothingOf.Contains(probed))
                {
                    return Rows([], holds: 0);
                }

                return Rows(["{}"], holds: 1);
            }

            private HttpResponseMessage AnswerAboutBoards(string query)
            {
                if (boardTableStatus != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(boardTableStatus)
                    {
                        Content = new StringContent("{\"error\":{\"message\":\"denied\"}}", Encoding.UTF8, "application/json"),
                    };
                }

                // X-Total-Count is computed before the ACLs run, so it reports boards the account
                // will never be shown. Measured 2026-08-01: header 2, body 0.
                var everyBoard = boards.Count;

                var visible = sharedWithThisAccount ? SelectedBy(query) : [];

                return Rows([.. visible.Select(AsJson)], everyBoard);
            }

            private List<BoardRow> SelectedBy(string query)
            {
                var conditions = EncodedQueryIn(query).Split('^', StringSplitOptions.RemoveEmptyEntries);

                var selected = boards.AsEnumerable();

                foreach (var condition in conditions)
                {
                    selected = Narrow(selected, condition);
                }

                return [.. selected];
            }

            private static IEnumerable<BoardRow> Narrow(IEnumerable<BoardRow> selected, string condition)
            {
                if (condition == "active=true")
                {
                    return selected.Where(board => board.IsActive);
                }

                if (condition == "tableISNOTEMPTY")
                {
                    return selected.Where(board => board.Table.Length > 0);
                }

                if (condition == "filterISNOTEMPTY")
                {
                    return selected.Where(board => board.Filter.Length > 0);
                }

                if (condition.StartsWith("sys_id=", StringComparison.Ordinal))
                {
                    var wanted = condition["sys_id=".Length..];

                    return selected.Where(board => board.Id == wanted);
                }

                return selected;
            }

            private static string? NamedIn(string query)
            {
                foreach (var condition in EncodedQueryIn(query).Split('^', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (condition.StartsWith("sys_class_name=", StringComparison.Ordinal))
                    {
                        return condition["sys_class_name=".Length..];
                    }
                }

                return null;
            }

            private static string EncodedQueryIn(string query)
            {
                const string parameter = "sysparm_query=";

                var start = query.IndexOf(parameter, StringComparison.Ordinal);

                return start < 0 ? string.Empty : query[(start + parameter.Length)..];
            }

            private static HttpResponseMessage AnswerWith(TableAnswer answer)
            {
                if (answer.Status != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(answer.Status)
                    {
                        Content = new StringContent("{\"error\":{\"message\":\"denied\"}}", Encoding.UTF8, "application/json"),
                    };
                }

                return Rows([.. Enumerable.Range(0, answer.Visible).Select(_ => "{}")], answer.Holds);
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

            // A board row as sysparm_display_value=all returns it. readable_filter is present in the
            // payload exactly because nothing in Lighthouse may read it.
            private static string AsJson(BoardRow board)
            {
                return $$"""
                    {
                      "sys_id": { "display_value": "{{board.Id}}", "value": "{{board.Id}}" },
                      "name": { "display_value": "{{board.Name}}", "value": "{{board.Name}}" },
                      "table": { "display_value": "{{board.Table}}", "value": "{{board.Table}}" },
                      "filter": { "display_value": "{{board.ReadableFilter}}", "value": "{{board.Filter}}" },
                      "readable_filter": { "display_value": "{{board.ReadableFilter}}", "value": "{{board.ReadableFilter}}" },
                      "active": { "display_value": "true", "value": "true" }
                    }
                    """;
            }
        }
    }
}
